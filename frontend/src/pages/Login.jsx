// src/pages/Login.jsx
import React, { useState, useContext } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { AuthContext } from "../context/AuthContext";

export default function Login() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useContext(AuthContext);

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [err, setErr] = useState(null);
  const [loading, setLoading] = useState(false);

  // from — куда вернём пользователя после успешного логина
  const from = location.state?.from?.pathname || "/";

  const onSubmit = async (e) => {
    e.preventDefault();
    setErr(null);
    setLoading(true);
    try {
      await login({ email: email.trim(), password });
      navigate(from, { replace: true });
    } catch (error) {
      const status = error?.response?.status;
      const body = error?.response?.data;
      // если backend возвращает ValidationProblem/ModelState, можно разбирать body.errors
      if (status === 400 && body?.errors) {
        // простой показ первой ошибки
        const firstField = Object.keys(body.errors)[0];
        setErr(body.errors[firstField]?.[0] ?? "Ошибка в данных");
      } else {
        setErr(body?.title || (status ? `Ошибка: ${status}` : error.message));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container py-5" style={{ maxWidth: 540 }}>
      <h2 className="mb-4">Вход</h2>
      {err && <div className="alert alert-danger">{err}</div>}
      <form onSubmit={onSubmit}>
        <div className="mb-3">
          <label className="form-label">Email</label>
          <input
            className="form-control"
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            disabled={loading}
            required
          />
        </div>
        <div className="mb-3">
          <label className="form-label">Пароль</label>
          <input
            className="form-control"
            type="password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            disabled={loading}
            required
          />
        </div>
        <button className="btn btn-primary" type="submit" disabled={loading}>
          {loading ? "Входим..." : "Войти"}
        </button>
      </form>
    </div>
  ); 
}
