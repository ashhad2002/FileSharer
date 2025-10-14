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

import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import File from '../component/File';
import httpClient from '../httpClient';

describe('File Component', () => {
  const mockFileItem = {
    fileId: 1,
    fileName: 'test-file.jpg',
    uploaderID: 1,
    uploaderName: 'testuser',
    uploadDate: '2024-01-01T00:00:00Z',
    thumbnail: 'base64-encoded-thumbnail'
  };

  beforeEach(() => {
    jest.clearAllMocks();
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
    expect(thumbnail.src).toContain('data:image/png;base64,base64-encoded-thumbnail');
  });

  test('displays placeholder when thumbnail is not available', () => {
    const itemWithoutThumbnail = { ...mockFileItem, thumbnail: null };
    render(<File item={itemWithoutThumbnail} index={0} />);
    
    const thumbnail = screen.getByAltText('File');
    expect(thumbnail.src).toContain('landscape-placeholder.svg');
  });

  test('handles download successfully', async () => {
    const mockResponse = {
      data: {
        downloadUrl: 'https://example.com/download',
        fileName: 'test-file.jpg'
      }
    };
    
    httpClient.get.mockResolvedValue(mockResponse);
    
    const mockLink = {
      href: '',
      setAttribute: jest.fn(),
      click: jest.fn()
    };
    const createElementSpy = jest.spyOn(document, 'createElement').mockReturnValue(mockLink);
    const appendChildSpy = jest.spyOn(document.body, 'appendChild').mockImplementation(() => {});
    const removeChildSpy = jest.spyOn(document.body, 'removeChild').mockImplementation(() => {});

    const { container } = render(<File item={mockFileItem} index={0} />);
    
    const downloadButton = screen.getByText('Download');
    fireEvent.click(downloadButton);

    await waitFor(() => {
      expect(httpClient.get).toHaveBeenCalledWith('/downloadurl?fileId=1');
      expect(createElementSpy).toHaveBeenCalledWith('a');
      expect(mockLink.href).toBe('https://example.com/download');
      expect(mockLink.setAttribute).toHaveBeenCalledWith('download', 'test-file.jpg');
      expect(mockLink.click).toHaveBeenCalled();
    });

    createElementSpy.mockRestore();
    appendChildSpy.mockRestore();
    removeChildSpy.mockRestore();
  });

  test('shows loading state during download', async () => {
    httpClient.get.mockImplementation(() => new Promise(() => {}));

    const { container } = render(<File item={mockFileItem} index={0} />);
    
    const downloadButton = screen.getByText('Download');
    fireEvent.click(downloadButton);

    await waitFor(() => {
      expect(screen.getByRole('button')).toBeDisabled();
    });
  });

  test('handles download error', async () => {
    const consoleErrorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
    const alertSpy = jest.spyOn(window, 'alert').mockImplementation(() => {});
    
    httpClient.get.mockRejectedValue(new Error('Download failed'));

    const { container } = render(<File item={mockFileItem} index={0} />);
    
    const downloadButton = screen.getByText('Download');
    fireEvent.click(downloadButton);

    await waitFor(() => {
      expect(consoleErrorSpy).toHaveBeenCalledWith('Download error:', expect.any(Error));
      expect(alertSpy).toHaveBeenCalledWith('Failed to download file. Please try again.');
    });

    consoleErrorSpy.mockRestore();
    alertSpy.mockRestore();
  });
});
