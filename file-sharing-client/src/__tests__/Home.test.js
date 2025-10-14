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

import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import Home from '../pages/Home';
import httpClient from '../httpClient';

const localStorageMock = {
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
};
Object.defineProperty(window, 'localStorage', { value: localStorageMock });

delete window.location;
window.location = { reload: jest.fn() };

const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

const renderWithRouter = (component) => {
  return render(
    <BrowserRouter>
      {component}
    </BrowserRouter>
  );
};

describe('Home Component', () => {
  const mockFiles = [
    {
      fileId: 1,
      fileName: 'test-file1.jpg',
      uploaderID: 1,
      uploaderName: 'user1',
      uploadDate: '2024-01-01T00:00:00Z',
      thumbnail: 'base64-thumbnail1'
    },
    {
      fileId: 2,
      fileName: 'test-file2.pdf',
      uploaderID: 2,
      uploaderName: 'user2',
      uploadDate: '2024-01-02T00:00:00Z',
      thumbnail: null
    }
  ];

  beforeEach(() => {
    jest.clearAllMocks();
    httpClient.get.mockResolvedValue({ data: mockFiles });
    localStorageMock.getItem.mockImplementation((key) => {
      if (key === 'token') return null;
      return null;
    });
  });

  test('renders home page with title', () => {
    localStorageMock.getItem.mockImplementation((key) => {
      if (key === 'token') return 'fake-token';
      return null;
    });

    renderWithRouter(<Home />);
    
    expect(screen.getByText('File Share')).toBeInTheDocument();
    expect(screen.getByText('Upload a file')).toBeInTheDocument();
    expect(screen.getByText('Uploaded Files')).toBeInTheDocument();
  });

  test('shows login and register buttons when not logged in', () => {
    localStorageMock.getItem.mockReturnValue(null);
    renderWithRouter(<Home />);
    
    expect(screen.getByText('Register')).toBeInTheDocument();
    expect(screen.getByText('Login')).toBeInTheDocument();
  });

  test('shows logout button when logged in', () => {
    localStorageMock.getItem.mockImplementation((key) => {
      if (key === 'token') return 'fake-token';
      return null;
    });

    renderWithRouter(<Home />);
    
    expect(screen.getByRole('button', { name: /logout/i })).toBeInTheDocument();
  });

  test('loads and displays files on mount', async () => {
    renderWithRouter(<Home />);
    
    await waitFor(() => {
      expect(httpClient.get).toHaveBeenCalledWith('/files');
      expect(screen.getByText('test-file1.jpg')).toBeInTheDocument();
      expect(screen.getByText('test-file2.pdf')).toBeInTheDocument();
    });
  });

  test('filters files based on search term', async () => {
    renderWithRouter(<Home />);
    
    await waitFor(() => {
      expect(screen.getByText('test-file1.jpg')).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText('Search files...');
    fireEvent.change(searchInput, { target: { value: 'pdf' } });

    expect(screen.queryByText('test-file1.jpg')).not.toBeInTheDocument();
    expect(screen.getByText('test-file2.pdf')).toBeInTheDocument();
  });

  test('handles logout correctly', async () => {
    localStorageMock.getItem.mockImplementation((key) => {
      if (key === 'token') return 'fake-token';
      return null;
    });
    
    renderWithRouter(<Home />);
    
    const logoutButton = screen.getByRole('button', { name: /logout/i });
    
    await act(async () => {
      fireEvent.click(logoutButton);
    });

    expect(localStorageMock.removeItem).toHaveBeenCalledWith('token');
    expect(window.location.reload).toHaveBeenCalled();
  });

  test('renders upload section', () => {
    renderWithRouter(<Home />);
    
    expect(screen.getByText('Upload a file')).toBeInTheDocument();
    expect(screen.getByText('Drag & drop files here')).toBeInTheDocument();
    expect(screen.getByText('Browse Files')).toBeInTheDocument();
  });
});
