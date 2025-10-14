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
import Register from '../pages/Register';
import httpClient from '../httpClient';

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

describe('Register Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  test('renders registration form', () => {
    renderWithRouter(<Register />);
    
    expect(screen.getByText('Register')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Enter your username')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Enter your email')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Enter your password')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /submit/i })).toBeInTheDocument();
  });

  test('handles successful registration', async () => {
    httpClient.post.mockResolvedValue({ data: {}, status: 200 });

    renderWithRouter(<Register />);
    
    const usernameInput = screen.getByPlaceholderText('Enter your username');
    const emailInput = screen.getByPlaceholderText('Enter your email');
    const passwordInput = screen.getByPlaceholderText('Enter your password');
    const registerButton = screen.getByRole('button', { name: /submit/i });

    fireEvent.change(usernameInput, { target: { value: 'newuser' } });
    fireEvent.change(emailInput, { target: { value: 'newuser@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });
    fireEvent.click(registerButton);

    await waitFor(() => {
      expect(httpClient.post).toHaveBeenCalledWith('/register', {
        username: 'newuser',
        email: 'newuser@example.com',
        password: 'password123'
      });
      expect(mockNavigate).toHaveBeenCalledWith('/login');
    });
  });

  test('handles registration error', async () => {
    httpClient.post.mockRejectedValue(new Error('Username already exists'));

    renderWithRouter(<Register />);
    
    const usernameInput = screen.getByPlaceholderText('Enter your username');
    const emailInput = screen.getByPlaceholderText('Enter your email');
    const passwordInput = screen.getByPlaceholderText('Enter your password');
    const registerButton = screen.getByRole('button', { name: /submit/i });

    fireEvent.change(usernameInput, { target: { value: 'existinguser' } });
    fireEvent.change(emailInput, { target: { value: 'existinguser@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });
    fireEvent.click(registerButton);

    await waitFor(() => {
      expect(httpClient.post).toHaveBeenCalledWith('/register', {
        username: 'existinguser',
        email: 'existinguser@example.com',
        password: 'password123'
      });
      expect(mockNavigate).not.toHaveBeenCalled();
    });
  });

  test('validates required fields', async () => {
    renderWithRouter(<Register />);
    
    const submitButton = screen.getByRole('button', { name: /submit/i });
    fireEvent.click(submitButton);

    expect(httpClient.post).not.toHaveBeenCalled();
  });

  test('validates email format', async () => {
    renderWithRouter(<Register />);
    
    const usernameInput = screen.getByPlaceholderText('Enter your username');
    const emailInput = screen.getByPlaceholderText('Enter your email');
    const passwordInput = screen.getByPlaceholderText('Enter your password');
    const registerButton = screen.getByRole('button', { name: /submit/i });

    fireEvent.change(usernameInput, { target: { value: 'testuser' } });
    fireEvent.change(emailInput, { target: { value: 'invalid-email' } });
    fireEvent.change(passwordInput, { target: { value: 'password123' } });
    fireEvent.click(registerButton);

    await waitFor(() => {
      expect(httpClient.post).toHaveBeenCalledWith('/register', {
        username: 'testuser',
        email: 'invalid-email',
        password: 'password123'
      });
    });
  });
});
