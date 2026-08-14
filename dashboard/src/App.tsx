import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'

function LoginPagePlaceholder() {
  return <div>Login</div>
}

function DevicesPagePlaceholder() {
  return <div>Devices</div>
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<LoginPagePlaceholder />} />
        <Route path="/devices" element={<DevicesPagePlaceholder />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App
