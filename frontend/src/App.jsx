// src/App.jsx
import React from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Navbar from './components/Navbar';
import Register from './pages/Register';
import Login from './pages/Login';
import RecipesList from './pages/RecipesList';
import AdminPanel from './pages/AdminPanel';
import ProtectedRoute from './components/ProtectedRoute';
import RecipeCreate from './pages/RecipeCreate';
import RecipeDetails from './pages/RecipeDetails';
import Categories from './pages/Categories';
import Collections from './pages/Collections';

export default function App() {
  return (
    <BrowserRouter>
      <div className="app-root">
        <Navbar />
        {/* main отвечает за расположение контента, site-container используется внутри страниц */}
        <main>
          <Routes>
            <Route path="/register" element={<Register />} />
            <Route path="/login" element={<Login />} />
            <Route path="/" element={<RecipesList />} />
            <Route path="/recipes/create" element={<RecipeCreate />} />
            <Route path="/categories" element={<Categories />} />
            <Route path="/collections" element={<Collections />} />
            <Route
              path="/adminpanel"
              element={
                <ProtectedRoute requiredRole="Admin">
                  <AdminPanel />
                </ProtectedRoute>
              }
            />
            <Route path="/recipes/:id" element={<RecipeDetails />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}
