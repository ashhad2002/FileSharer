import { render, screen } from '@testing-library/react';
import App from '../App';

jest.mock('../pages/Home', () => {
  return function MockHome() {
    return <div data-testid="home-page">Home Page</div>;
  };
});

jest.mock('../pages/Register', () => {
  return function MockRegister() {
    return <div data-testid="register-page">Register Page</div>;
  };
});

jest.mock('../pages/Login', () => {
  return function MockLogin() {
    return <div data-testid="login-page">Login Page</div>;
  };
});

describe('App', () => {
  test('renders home page by default', () => {
    render(<App />);
    expect(screen.getByTestId('home-page')).toBeInTheDocument();
  });

  test('renders without crashing', () => {
    render(<App />);
    expect(screen.getByTestId('home-page')).toBeInTheDocument();
  });
});
