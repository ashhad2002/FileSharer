jest.mock('../httpClient', () => ({
  get: jest.fn(),
  post: jest.fn(),
  defaults: {
    baseURL: 'http://localhost:5166',
    timeout: 10000,
    headers: {
      'Content-Type': 'application/json'
    }
  }
}));

import { render, screen, fireEvent, waitFor, cleanup } from '@testing-library/react';
import File from '../component/File';
import httpClient from '../httpClient';
import { act } from 'react'; 

describe('File Component', () => {
  const mockFileItem = {
    fileId: 1,
    fileName: 'test-file.jpg',
    uploaderID: 1,
    uploaderName: 'testuser',
    uploadDate: '2024-01-01T00:00:00Z',
    thumbnail: 'base64-encoded-thumbnail'
  };
  let consoleErrorSpy;
  let alertSpy;

  beforeEach(() => {
    jest.clearAllMocks();
    consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
    alertSpy = jest.spyOn(window, 'alert').mockImplementation(() => {});
  });

  afterEach(() => {
    cleanup();
    consoleErrorSpy.mockRestore();
    alertSpy.mockRestore();
  });

  test('renders file information correctly', () => {
    render(<File item={mockFileItem} index={0} />);
    expect(screen.getByText('test-file.jpg')).toBeInTheDocument();
    expect(screen.getByText(/ID:\s*1/)).toBeInTheDocument();
    expect(screen.getByText(/Uploader:\s*testuser/)).toBeInTheDocument();
    expect(screen.getByText('Download')).toBeInTheDocument();
  });

  test('displays thumbnail when available', () => {
    render(<File item={mockFileItem} index={0} />);
    const thumbnail = screen.getByAltText('File');
    expect(thumbnail).toBeInTheDocument();
    expect(thumbnail).toHaveAttribute('src', 'data:image/png;base64,base64-encoded-thumbnail');
  });

  test('displays placeholder when thumbnail is not available', () => {
    const itemWithoutThumbnail = { ...mockFileItem, thumbnail: null };
    render(<File item={itemWithoutThumbnail} index={0} />);
    const thumbnail = screen.getByAltText('File');
    expect(thumbnail).toBeInTheDocument();
    expect(thumbnail).toHaveAttribute('src', expect.stringContaining('landscape-placeholder.svg'));
  });

  test('handles download successfully', async () => {
    const mockResponse = {
      data: {
        downloadUrl: 'https://example.com/download',
        fileName: 'test-file.jpg'
      }
    };
    render(<File item={mockFileItem} index={0} />);
    httpClient.get.mockResolvedValue(mockResponse);
    const mockLink = {
      href: '',
      setAttribute: jest.fn(),
      click: jest.fn()
    };
    const createElementSpy = jest.spyOn(document, 'createElement').mockReturnValue(mockLink);
    const appendChildSpy = jest.spyOn(document.body, 'appendChild').mockImplementation(() => mockLink);
    const removeChildSpy = jest.spyOn(document.body, 'removeChild').mockImplementation(() => {});

    try {
      const downloadButton = screen.getByText('Download');
      fireEvent.click(downloadButton);

      await waitFor(() => {
        expect(httpClient.get).toHaveBeenCalledWith('/downloadurl?fileId=1');
        expect(createElementSpy).toHaveBeenCalledWith('a');
        expect(mockLink.href).toBe('https://example.com/download');
        expect(mockLink.setAttribute).toHaveBeenCalledWith('download', 'test-file.jpg');
        expect(mockLink.setAttribute).toHaveBeenCalledWith('target', '_blank');
        expect(mockLink.click).toHaveBeenCalled();
        expect(appendChildSpy).toHaveBeenCalledWith(mockLink);
        expect(removeChildSpy).toHaveBeenCalledWith(mockLink);
      });
    } finally {
      createElementSpy.mockRestore();
      appendChildSpy.mockRestore();
      removeChildSpy.mockRestore();
    }
  });

  test('shows loading state during download', async () => {
    let resolvePromise;
    
    const deferredPromise = new Promise((resolve, _) => {
      resolvePromise = resolve;
    });

    httpClient.get.mockImplementation(() => deferredPromise);

    render(<File item={mockFileItem} index={0} />);
    const downloadButton = screen.getByText('Download');
    fireEvent.click(downloadButton);

    await waitFor(() => {
      expect(screen.getByRole('button')).toBeDisabled();
    });
    await act(async () => {
        resolvePromise({ data: { downloadUrl: 'https://example.com/download', fileName: 'test-file.jpg' } });
        await Promise.resolve();
    });
    
    await waitFor(() => {
      expect(screen.getByRole('button')).not.toBeDisabled();
    });
  });

  test('handles download error', async () => {
    httpClient.get.mockRejectedValue(new Error('Download failed'));

    render(<File item={mockFileItem} index={0} />);

    const downloadButton = screen.getByText('Download');
    fireEvent.click(downloadButton);

    // Wait for the asynchronous error handling (API rejection) to complete
    await waitFor(() => {
      expect(consoleErrorSpy).toHaveBeenCalledWith('Download error:', expect.any(Error));
      expect(alertSpy).toHaveBeenCalledWith('Failed to download file. Please try again.');
    });

    // Ensure the button is enabled again after the error state resolves
    expect(screen.getByRole('button')).not.toBeDisabled();
  });
});
