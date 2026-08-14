import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import App from './App'

describe('App', () => {
  it('redirects / to the login page', () => {
    render(<App />)
    expect(screen.getByText('Login')).toBeInTheDocument()
  })
})
