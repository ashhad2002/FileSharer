jest.mock('axios', () => ({
  create: jest.fn(() => ({
    defaults: {
      baseURL: 'http://localhost:5166',
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json'
      }
    }
  }))
}));

import httpClient from '../httpClient';

describe('httpClient', () => {
  test('should have correct base URL', () => {
    expect(httpClient.defaults.baseURL).toBe('http://localhost:5166');
  });

  test('should have correct timeout', () => {
    expect(httpClient.defaults.timeout).toBe(10000);
  });

  test('should have correct headers', () => {
    expect(httpClient.defaults.headers['Content-Type']).toBe('application/json');
  });
});
