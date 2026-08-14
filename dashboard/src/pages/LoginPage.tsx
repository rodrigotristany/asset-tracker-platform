import { useState, type FormEvent } from "react";
import { useMutation } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "../store/authStore";

export function LoginPage() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const navigate = useNavigate();
  const login = useAuthStore((state) => state.login);

  const mutation = useMutation<void, Error, { username: string; password: string }>({
    mutationFn: ({ username, password }) => login(username, password),
    onSuccess: () => navigate("/devices"),
  });

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault();
    mutation.mutate({ username, password });
  };

  return (
    <form onSubmit={handleSubmit}>
      <h1>Sign in</h1>
      <label htmlFor="username">Username</label>
      <input
        id="username"
        value={username}
        onChange={(event) => setUsername(event.target.value)}
        required
      />
      <label htmlFor="password">Password</label>
      <input
        id="password"
        type="password"
        value={password}
        onChange={(event) => setPassword(event.target.value)}
        required
      />
      <button type="submit" disabled={mutation.isPending}>
        Sign in
      </button>
      {mutation.isError && <p role="alert">Invalid username or password</p>}
    </form>
  );
}
