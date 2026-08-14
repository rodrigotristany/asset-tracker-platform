import { render, screen } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { describe, expect, it } from 'vitest'
import App from './App'

describe('App', () => {
  it('redirects / to the login page', () => {
    const queryClient = new QueryClient()
    render(
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>,
    )
    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument()
  })
})
