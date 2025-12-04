// src/pages/Register.jsx
import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { register } from "../services/auth";

export default function Register() {
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState({});
  const [generalError, setGeneralError] = useState(null);

  const validateClient = () => {
    const e = {};
    if (!email.trim()) e.email = ["Email обязателен"];
    if (email && !/^\S+@\S+\.\S+$/.test(email)) e.email = (e.email || []).concat("Неверный формат email");
    if (!password) e.password = ["Пароль обязателен"];
    if (password && password.length < 6) e.password = (e.password || []).concat("Пароль должен быть не менее 6 символов");
    if (password !== confirm) e.confirm = ["Пароли не совпадают"];
    return e;
  };

  const onSubmit = async (ev) => {
    ev.preventDefault();
    setErrors({});
    setGeneralError(null);

    const clientErr = validateClient();
    if (Object.keys(clientErr).length) {
      setErrors(clientErr);
      return;
    }

    setLoading(true);
    try {
      // Если бек возвращает 204 No Content — axios вернёт status 204 и data === ''
      await register({ email: email.trim(), password });
      navigate("/login", { replace: true, state: { registered: true } });
    } catch (err) {
      console.error(err);
      const status = err?.response?.status;
      const data = err?.response?.data;

      if (status === 400 && data?.errors) {
        setErrors(data.errors);
      } else if (data?.title) {
        setGeneralError(data.title);
      } else if (status) {
        setGeneralError(`Ошибка: ${status}`);
      } else {
        setGeneralError(err.message ?? "Неизвестная ошибка");
      }
    } finally {
      setLoading(false);
    }
  };

  const renderFieldErrors = (field) => {
    const f = errors?.[field];
    if (!f || !f.length) return null;
    return f.map((m, i) => <div className="invalid-feedback d-block" key={i}>{m}</div>);
  };

  return (
    <div className="container py-5" style={{ maxWidth: 600 }}>
      <h2 className="mb-4">Регистрация</h2>
      {generalError && <div className="alert alert-danger">{generalError}</div>}
      <form onSubmit={onSubmit} noValidate>
        <div className="mb-3">
          <label className="form-label">Email</label>
          <input type="email" className={`form-control ${errors.email ? "is-invalid" : ""}`}
                 value={email} onChange={e => setEmail(e.target.value)} disabled={loading} />
          {renderFieldErrors("email")}
        </div>
        <div className="mb-3">
          <label className="form-label">Пароль</label>
          <input type="password" className={`form-control ${errors.password ? "is-invalid" : ""}`}
                 value={password} onChange={e => setPassword(e.target.value)} disabled={loading} />
          {renderFieldErrors("password")}
        </div>
        <div className="mb-3">
          <label className="form-label">Подтвердите пароль</label>
          <input type="password" className={`form-control ${errors.confirm ? "is-invalid" : ""}`}
                 value={confirm} onChange={e => setConfirm(e.target.value)} disabled={loading} />
          {renderFieldErrors("confirm")}
        </div>
        <button className="btn btn-primary" type="submit" disabled={loading}>
          {loading ? "Регистрация..." : "Зарегистрироваться"}
        </button>
      </form>
    </div>
  );
}
