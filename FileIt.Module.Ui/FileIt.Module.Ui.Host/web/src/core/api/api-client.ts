const API_BASE = '/api';

export async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`);

  if (!response.ok) {
    throw new Error(`${path}: ${response.status}`);
  }

  return response.json();
}
