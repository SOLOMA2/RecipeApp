import React, { useContext, useEffect, useState } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { AuthContext } from "../context/AuthContext";

function BrandIcon({ className }) {
  return (
    <svg className={className} width="32" height="32" viewBox="0 0 48 48" aria-hidden>
      <path 
        d="M15 24c0-5.5 6-9 9-9s9 3.5 9 9c0 0 0 2.5-2 4s-3 2-7 2-9-1.5-9-6z M24 14c-1.6 0-3 1.4-3 3v2h6v-2c0-1.6-1.4-3-3-3z" 
        fill="currentColor"
      />
    </svg>
  );
}

function Avatar({ name }) {
  const initials = (name || "U").split(" ").map(s => s[0]).slice(0, 2).join("").toUpperCase();
  return (
    <div className="avatar-circle" title={name}>
      {initials}
    </div>
  );
}

export default function Navbar() {
  const { isAuthenticated, user, logout } = useContext(AuthContext);
  const navigate = useNavigate();
  const [theme, setTheme] = useState(() => localStorage.getItem("theme") || "light");
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const isAdmin = (user?.roles ?? []).includes("Admin");

  useEffect(() => {
    document.documentElement.setAttribute("data-theme", theme);
    localStorage.setItem("theme", theme);
  }, [theme]);

  const toggleTheme = () => setTheme(t => t === "light" ? "dark" : "light");

  const onLogout = async () => {
    await logout();
    navigate("/", { replace: true });
    setMobileMenuOpen(false);
  };

  const NavLinkItem = ({ to, children }) => (
    <NavLink 
      to={to} 
      className={({isActive}) => `nav-link ${isActive ? "active" : ""}`}
      onClick={() => setMobileMenuOpen(false)}
    >
      {children}
    </NavLink>
  );

  return (
    <header className={`site-header ${mobileMenuOpen ? "mobile-open" : ""}`}>
      <div className="header-inner">
        <NavLink to="/" className="brand" aria-label="RecipeBook — главная">
          <BrandIcon className="brand-logo" />
          <span className="brand-title">RecipeBook</span>
        </NavLink>

        <nav className={`main-nav ${mobileMenuOpen ? "open" : ""}`} aria-label="Навигация">
          <NavLinkItem to="/">Рецепты</NavLinkItem>
          <NavLinkItem to="/categories">Категории</NavLinkItem>
          <NavLinkItem to="/collections">Коллекции</NavLinkItem>
        </nav>

        <div className="header-actions">
          <button className="btn icon-btn theme-toggle" onClick={toggleTheme} aria-label="Переключить тему">
            {theme === "light" ? "🌙" : "☀️"}
          </button>

          {isAuthenticated ? (
            <div className="header-auth">
              <div className="header-auth-buttons">
                <button 
                  className="btn btn-primary create-btn" 
                  onClick={() => {
                    navigate("/recipes/create");
                    setMobileMenuOpen(false);
                  }}
                >
                  <span className="btn-icon">+</span>
                  Создать
                </button>

                {isAdmin && (
                  <button
                    className="btn btn-ghost admin-link"
                    onClick={() => {
                      navigate("/adminpanel");
                      setMobileMenuOpen(false);
                    }}
                  >
                    Панель
                  </button>
                )}
              </div>

              <div className="profile-dropdown">
                <button className="profile-trigger" aria-haspopup="true" aria-expanded="false">
                  <Avatar name={user?.name ?? user?.email} />
                </button>
                <div className="dropdown-menu">
                  <div className="dropdown-label">
                    <div className="dropdown-name">{user?.name ?? "Пользователь"}</div>
                    <div className="dropdown-email">{user?.email}</div>
                  </div>
                  <button onClick={() => navigate("/profile")}>Мой профиль</button>
                  <button className="logout-btn" onClick={onLogout}>Выйти</button>
                </div>
              </div>
            </div>
          ) : (
            <div className="auth-buttons">
              <button className="btn btn-ghost" onClick={() => navigate("/login")}>Войти</button>
              <button className="btn btn-primary" onClick={() => navigate("/register")}>Регистрация</button>
            </div>
          )}
        </div>

        <button 
          className={`mobile-toggle ${mobileMenuOpen ? "open" : ""}`}
          type="button" 
          aria-label="Открыть меню" 
          onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
        >
          <span></span>
          <span></span>
          <span></span>
        </button>
      </div>
    </header>
  );
}