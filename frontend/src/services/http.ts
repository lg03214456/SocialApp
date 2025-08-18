export const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://127.0.0.1:5000'

export async function getForecast() {
  const res = await fetch(`${API_BASE}/weatherforecast`)
  if (!res.ok) throw new Error(`API ${res.status}`)
  return res.json()
}
