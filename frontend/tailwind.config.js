/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{vue,ts,tsx}'],
  darkMode: 'class',
  theme: { extend: {
    fontFamily: {
        sans: ['Montserrat', 'Noto Sans TC', 'system-ui', 'Segoe UI', 'Roboto', 'Helvetica Neue', 'Arial'],
        brand: ['Montserrat', 'Noto Sans TC', 'sans-serif'],
      },
  } },
  plugins: [],
}
