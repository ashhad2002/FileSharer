// Mock httpClient before importing any components
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
import Login from '../pages/Login';
import httpClient from '../httpClient';

const localStorageMock = {
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
};
global.localStorage = localStorageMock;

Object.defineProperty(window, 'localStorage', { value: localStorageMock });

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

describe('Login Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('renders login form', () => {
    renderWithRouter(<Login />);
    
    expect(screen.getByText('Login')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Enter your username')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Enter your password')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /submit/i })).toBeInTheDocument();
  });

  test('handles successful login', async () => {
    const mockResponse = {
      data: {
        token: 'fake-jwt-token',
        userName: 'testuser'
      },
      status: 200
    };

    
    httpClient.post.mockResolvedValue(mockResponse);

    renderWithRouter(<Login />);
    
    const usernameInput = screen.getByPlaceholderText('Enter your username');
    const passwordInput = screen.getByPlaceholderText('Enter your password');
    const loginButton = screen.getByRole('button', { name: /submit/i });

    await act(async () => {
      fireEvent.change(usernameInput, { target: { value: 'testuser' } });
      fireEvent.change(passwordInput, { target: { value: 'password123' } });
      fireEvent.click(loginButton);
    });

    await waitFor(() => {
      expect(httpClient.post).toHaveBeenCalledWith('/login', {
        username: 'testuser',
        password: 'password123'
      });
      expect(localStorageMock.setItem).toHaveBeenCalledWith('token', 'fake-jwt-token');
      expect(mockNavigate).toHaveBeenCalledWith('/');
    });
  });

  test('handles login error', async () => {
    httpClient.post.mockRejectedValue(new Error('Invalid credentials'));

    renderWithRouter(<Login />);
    
    const usernameInput = screen.getByPlaceholderText('Enter your username');
    const passwordInput = screen.getByPlaceholderText('Enter your password');
    const loginButton = screen.getByRole('button', { name: /submit/i });

    await act(async () => {
      fireEvent.change(usernameInput, { target: { value: 'testuser' } });
      fireEvent.change(passwordInput, { target: { value: 'wrongpassword' } });
      fireEvent.click(loginButton);
    });

    await waitFor(() => {
      expect(httpClient.post).toHaveBeenCalledWith('/login', {
        username: 'testuser',
        password: 'wrongpassword'
      });
      expect(localStorageMock.setItem).not.toHaveBeenCalled();
      expect(mockNavigate).not.toHaveBeenCalled();
    });
  });

  test('validates required fields', async () => {
    renderWithRouter(<Login />);
    
    const submitButton = screen.getByRole('button', { name: /submit/i });
    fireEvent.click(submitButton);

    expect(httpClient.post).not.toHaveBeenCalled();
  });
});
