import React, { useState, useContext, useMemo, useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { AuthContext } from "../context/AuthContext";
import api from "../api/http";

const formatMacroValue = (value) => {
  const num = Number(value);
  if (!Number.isFinite(num) || num === 0) return "—";
  return num.toFixed(1);
};

const unitToGrams = (amount, unit) => {
  const value = parseFloat(String(amount).replace(",", "."));
  if (Number.isNaN(value) || value <= 0) return 0;
  switch ((unit || "").toLowerCase()) {
    case "g":
      return value;
    case "kg":
      return value * 1000;
    case "ml":
      return value;
    case "l":
      return value * 1000;
    case "tsp":
      return value * 5;
    case "tbsp":
      return value * 15;
    case "cup":
      return value * 240;
    default:
      return value;
  }
};

const createIngredient = () => ({
  id: crypto.randomUUID(),
  amount: "",
  unit: "g",
  name: "",
  weight: "",
  calories: "",
  protein: "",
  fat: "",
  carbs: "",
});

const computeIngredientWeight = (ingredient) => {
  if (!ingredient) return 0;
  if (ingredient.weight && Number(ingredient.weight) > 0) {
    return Number(ingredient.weight);
  }
  return unitToGrams(ingredient.amount, ingredient.unit);
};

export default function RecipeCreate() {
  const { isAuthenticated } = useContext(AuthContext);
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState({});

  // Основные данные рецепта
  const [formData, setFormData] = useState({
    title: "",
    description: "",
    prepTime: "",
    cookTime: "",
    servings: "",
    difficulty: "medium",
    category: "",
    imageUrl: ""
  });

  // Динамические поля
  const [ingredients, setIngredients] = useState([createIngredient()]);
  const [instructions, setInstructions] = useState([{ id: crypto.randomUUID(), text: "" }]);
  const [tags, setTags] = useState([]);
  const [newTag, setNewTag] = useState("");
  const [showNutritionPanel, setShowNutritionPanel] = useState(true);
  const [autoSaveEnabled, setAutoSaveEnabled] = useState(true);
  const [lastSaved, setLastSaved] = useState(null);
  const [categories, setCategories] = useState([]);
  const [loadingCategories, setLoadingCategories] = useState(false);
  const formRef = useRef(null);

  // Загрузка категорий
  useEffect(() => {
    const loadCategories = async () => {
      setLoadingCategories(true);
      try {
        const response = await api.get('/categories?pageSize=100');
        const payload = response?.data ?? response;
        const items = Array.isArray(payload?.items)
          ? payload.items
          : Array.isArray(payload)
          ? payload
          : [];
        
        setCategories(items);
      } catch (err) {
        console.warn('Failed to load categories:', err);
        setCategories([]);
        setErrors(prev => ({ ...prev, category: "Не удалось загрузить категории" }));
      } finally {
        setLoadingCategories(false);
      }
    };
    loadCategories();
  }, []);

  // Автосохранение в localStorage
  useEffect(() => {
    if (!autoSaveEnabled) return;
    
    const saveData = {
      formData,
      ingredients,
      instructions,
      tags
    };
    
    try {
      localStorage.setItem('recipe-draft', JSON.stringify(saveData));
      setLastSaved(new Date());
    } catch (err) {
      console.warn('Failed to save draft:', err);
    }
  }, [formData, ingredients, instructions, tags, autoSaveEnabled]);

  // Загрузка черновика при монтировании
  useEffect(() => {
    try {
      const saved = localStorage.getItem('recipe-draft');
      if (saved) {
        const data = JSON.parse(saved);
        if (data.formData) setFormData(data.formData);
        if (data.ingredients && data.ingredients.length > 0) setIngredients(data.ingredients);
        if (data.instructions && data.instructions.length > 0) setInstructions(data.instructions);
        if (data.tags) setTags(data.tags);
      }
    } catch (err) {
      console.warn('Failed to load draft:', err);
    }
  }, []);

  // Очистка черновика после успешной отправки
  const clearDraft = () => {
    localStorage.removeItem('recipe-draft');
    setAutoSaveEnabled(false);
  };

  const resolveIngredientWeight = (ingredient) => computeIngredientWeight(ingredient);
  
  const nutritionTotals = useMemo(() => {
    return ingredients.reduce((acc, ing) => {
      acc.calories += Number(ing.calories) || 0;
      acc.protein += Number(ing.protein) || 0;
      acc.fat += Number(ing.fat) || 0;
      acc.carbs += Number(ing.carbs) || 0;
      acc.weight += resolveIngredientWeight(ing) || 0;
      return acc;
    }, { calories: 0, protein: 0, fat: 0, carbs: 0, weight: 0 });
  }, [ingredients]);

  const nutritionPer100 = useMemo(() => {
    const weight = nutritionTotals.weight > 0 ? nutritionTotals.weight : 100;
    const factor = weight > 0 ? 100 / weight : 0;
    return {
      calories: +(nutritionTotals.calories * factor).toFixed(2) || 0,
      protein: +(nutritionTotals.protein * factor).toFixed(2) || 0,
      fat: +(nutritionTotals.fat * factor).toFixed(2) || 0,
      carbs: +(nutritionTotals.carbs * factor).toFixed(2) || 0,
    };
  }, [nutritionTotals]);

  const nutritionPerServing = useMemo(() => {
    const servingsCount = (formData.servings && Number(formData.servings) > 0) ? Number(formData.servings) : 1;
    return {
      calories: +(nutritionTotals.calories / servingsCount).toFixed(1) || 0,
      protein: +(nutritionTotals.protein / servingsCount).toFixed(1) || 0,
      fat: +(nutritionTotals.fat / servingsCount).toFixed(1) || 0,
      carbs: +(nutritionTotals.carbs / servingsCount).toFixed(1) || 0,
      weight: +(nutritionTotals.weight / servingsCount).toFixed(0) || 0,
    };
  }, [nutritionTotals, formData.servings]);

  const nutritionAnalysis = useMemo(() => {
    const total = nutritionPerServing.calories || 1;
    const proteinPct = (nutritionPerServing.protein * 4 / total) * 100;
    const fatPct = (nutritionPerServing.fat * 9 / total) * 100;
    const carbsPct = (nutritionPerServing.carbs * 4 / total) * 100;
    
    const recommendations = [];
    
    if (proteinPct < 15) {
      recommendations.push({ type: 'info', text: 'Низкое содержание белка. Добавьте белковые продукты.' });
    } else if (proteinPct > 35) {
      recommendations.push({ type: 'warning', text: 'Очень высокое содержание белка.' });
    }
    
    if (fatPct < 20) {
      recommendations.push({ type: 'info', text: 'Низкое содержание жиров. Добавьте полезные жиры.' });
    } else if (fatPct > 40) {
      recommendations.push({ type: 'warning', text: 'Высокое содержание жиров.' });
    }
    
    if (carbsPct < 30) {
      recommendations.push({ type: 'info', text: 'Низкоуглеводный рецепт.' });
    }
    
    if (nutritionPerServing.calories < 200) {
      recommendations.push({ type: 'success', text: 'Низкокалорийный рецепт. Отлично для диеты!' });
    } else if (nutritionPerServing.calories > 600) {
      recommendations.push({ type: 'warning', text: 'Высококалорийный рецепт.' });
    }
    
    return {
      proteinPct: +proteinPct.toFixed(1),
      fatPct: +fatPct.toFixed(1),
      carbsPct: +carbsPct.toFixed(1),
      recommendations
    };
  }, [nutritionPerServing]);

  // Если пользователь не авторизован - редирект через useEffect,
  // а не во время первого рендера компонента
  useEffect(() => {
    if (!isAuthenticated) {
      navigate("/login", { replace: true });
    }
  }, [isAuthenticated, navigate]);

  if (!isAuthenticated) {
    return null;
  }

  // Обработчики для основных полей
  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
    if (errors[name]) {
      setErrors(prev => ({ ...prev, [name]: "" }));
    }
  };

  // Ингредиенты
  const updateIngredient = (id, patch) => {
    setIngredients(prev =>
      prev.map(item =>
        item.id === id ? { ...item, ...patch } : item
      )
    );
  };

  const addIngredient = () => {
    setIngredients(prev => [...prev, createIngredient()]);
    // Прокрутка к новому ингредиенту
    setTimeout(() => {
      const lastIngredient = document.querySelector('.ingredient-row:last-child');
      if (lastIngredient) {
        lastIngredient.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        const input = lastIngredient.querySelector('.ingredient-name');
        if (input) input.focus();
      }
    }, 100);
  };

  const duplicateIngredient = (id) => {
    const ingredient = ingredients.find(ing => ing.id === id);
    if (ingredient) {
      const duplicated = { ...ingredient, id: crypto.randomUUID() };
      setIngredients(prev => {
        const index = prev.findIndex(ing => ing.id === id);
        return [...prev.slice(0, index + 1), duplicated, ...prev.slice(index + 1)];
      });
    }
  };

  const removeIngredient = (id) => {
    if (ingredients.length > 1) {
      setIngredients(prev => prev.filter(item => item.id !== id));
    }
  };

  // Шаги приготовления
  const addInstruction = () => {
    setInstructions(prev => [
      ...prev,
      { id: crypto.randomUUID(), text: "" }
    ]);
    // Прокрутка к новому шагу
    setTimeout(() => {
      const lastInstruction = document.querySelector('.instruction-item:last-child textarea');
      if (lastInstruction) {
        lastInstruction.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        lastInstruction.focus();
      }
    }, 100);
  };

  const duplicateInstruction = (id) => {
    const instruction = instructions.find(inst => inst.id === id);
    if (instruction) {
      const duplicated = { ...instruction, id: crypto.randomUUID() };
      setInstructions(prev => {
        const index = prev.findIndex(inst => inst.id === id);
        return [...prev.slice(0, index + 1), duplicated, ...prev.slice(index + 1)];
      });
    }
  };

  const removeInstruction = (id) => {
    if (instructions.length > 1) {
      setInstructions(prev => prev.filter(item => item.id !== id));
    }
  };

  const updateInstruction = (id, text) => {
    setInstructions(prev => 
      prev.map(item => 
        item.id === id ? { ...item, text } : item
      )
    );
  };

  // Теги
  const addTag = () => {
    if (newTag.trim() && !tags.includes(newTag.trim())) {
      setTags(prev => [...prev, newTag.trim()]);
      setNewTag("");
    }
  };

  const removeTag = (tagToRemove) => {
    setTags(prev => prev.filter(tag => tag !== tagToRemove));
  };

  const handleKeyPress = (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      addTag();
    }
  };

  // Валидация формы
  const validateForm = () => {
    const newErrors = {};

    if (!formData.title.trim()) {
      newErrors.title = "Название обязательно";
    } else if (formData.title.trim().length < 3) {
      newErrors.title = "Название должно содержать минимум 3 символа";
    } else if (formData.title.trim().length > 200) {
      newErrors.title = "Название не должно превышать 200 символов";
    }

    if (!formData.description.trim()) {
      newErrors.description = "Описание обязательно";
    } else if (formData.description.trim().length < 10) {
      newErrors.description = "Описание должно содержать минимум 10 символов";
    } else if (formData.description.trim().length > 2000) {
      newErrors.description = "Описание не должно превышать 2000 символов";
    }

    const emptyIngredients = ingredients.filter(ing => !ing.name.trim());
    if (emptyIngredients.length > 0) {
      newErrors.ingredients = `Заполните названия для ${emptyIngredients.length} ингредиент${emptyIngredients.length > 1 ? 'ов' : 'а'}`;
    }

    const emptyInstructions = instructions.filter(inst => !inst.text.trim());
    if (emptyInstructions.length > 0) {
      newErrors.instructions = `Заполните ${emptyInstructions.length} шаг${emptyInstructions.length > 1 ? 'а' : ''} приготовления`;
    }

    // Валидация URL изображения
    if (formData.imageUrl && !/^https?:\/\/.+/.test(formData.imageUrl)) {
      newErrors.imageUrl = "Введите корректный URL изображения";
    }

    setErrors(newErrors);
    
    // Прокрутка к первой ошибке
    if (Object.keys(newErrors).length > 0) {
      const firstErrorField = Object.keys(newErrors)[0];
      const errorElement = document.querySelector(`[name="${firstErrorField}"], .section-error`);
      if (errorElement) {
        errorElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        if (errorElement.tagName === 'INPUT' || errorElement.tagName === 'TEXTAREA') {
          errorElement.focus();
        }
      }
    }

    return Object.keys(newErrors).length === 0;
  };

  // Отправка формы
  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateForm()) return;

    setLoading(true);
    setErrors({});

    try {
      const ingredientsDto = ingredients.map(ing => {
        const quantity = parseFloat(String(ing.amount).replace(',', '.')) || 0;
        const grams = resolveIngredientWeight(ing);
        return {
          title: (ing.name || "").trim(),
          description: "",
          calories: Number(ing.calories) || 0,
          protein: Number(ing.protein) || 0,
          fat: Number(ing.fat) || 0,
          carbohydrates: Number(ing.carbs) || 0,
          weight: grams || 0,
          quantity,
          unit: ing.unit || "",
        };
      });

      const tagsDto = tags.map(t => ({ title: t }));

      const categoryIds = [];
      if (formData.category && !isNaN(Number(formData.category))) {
        categoryIds.push(Number(formData.category));
      }

      const totalWeight = Number(nutritionTotals.weight.toFixed(2)) || 0;
      const totalCalories = Number(nutritionTotals.calories.toFixed(2)) || 0;
      const totalProtein = Number(nutritionTotals.protein.toFixed(2)) || 0;
      const totalFat = Number(nutritionTotals.fat.toFixed(2)) || 0;
      const totalCarbs = Number(nutritionTotals.carbs.toFixed(2)) || 0;

      // Объединяем инструкции в cookingMethod
      const cookingMethod = instructions
        .filter(inst => inst.text.trim())
        .map((inst, idx) => `${idx + 1}. ${inst.text.trim()}`)
        .join('\n\n');

      const payload = {
        title: formData.title.trim(),
        description: formData.description.trim() || null,
        imageUrl: formData.imageUrl?.trim() || null,
        weight: totalWeight,
        calories: totalCalories,
        protein: totalProtein,
        fat: totalFat,
        carbohydrates: totalCarbs,
        cookingMethod: cookingMethod || "Не указано",
        cookingTimeMinutes: Number(formData.cookTime) || 0,
        ingredients: ingredientsDto,
        categoryIds: categoryIds.length ? categoryIds : null,
        tags: tagsDto.length ? tagsDto : null
      };

      await api.post("/recipes", payload);
      clearDraft();
      navigate("/", { state: { message: "Рецепт успешно создан!" } });
    } catch (err) {
      console.error("Failed to create recipe:", err);

      const data = err?.response?.data;
      if (data?.errors) {
        const uiErr = {};
        for (const [key, messages] of Object.entries(data.errors)) {
          const last = key.toString().split('.').slice(-1)[0];
          const k = last.charAt(0).toLowerCase() + last.slice(1);
          uiErr[k] = messages;
        }
        setErrors(uiErr);
      } else if (data?.title) {
        setErrors({ submit: data.title });
      } else {
        setErrors({ submit: `Ошибка: ${err?.response?.status || err.message}` });
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="recipe-create-page">
      <div className="create-container">
        <div className="create-header">
          <button 
            className="back-btn"
            onClick={() => navigate(-1)}
            type="button"
          >
            ← Назад
          </button>
          <h1>Создать новый рецепт</h1>
          <p>Поделитесь своим кулинарным творением с сообществом</p>
          {lastSaved && autoSaveEnabled && (
            <div className="autosave-indicator">
              💾 Автосохранено {lastSaved.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}
            </div>
          )}
        </div>

        <form ref={formRef} onSubmit={handleSubmit} className="recipe-form">
          {/* Основная информация */}
          <section className="form-section">
            <h2>Основная информация</h2>
            
            <div className="form-grid">
              <div className="form-group full-width">
                <label htmlFor="title" className="required">
                  Название рецепта
                </label>
                <input
                  id="title"
                  name="title"
                  type="text"
                  value={formData.title}
                  onChange={handleInputChange}
                  placeholder="Например: Классический шоколадный торт"
                  className={errors.title ? "error" : ""}
                  maxLength={200}
                />
                {errors.title && <span className="error-message">{errors.title}</span>}
              </div>

              <div className="form-group full-width">
                <label htmlFor="description" className="required">
                  Описание
                </label>
                <textarea
                  id="description"
                  name="description"
                  value={formData.description}
                  onChange={handleInputChange}
                  placeholder="Расскажите о вашем рецепте, что в нем особенного..."
                  rows="4"
                  className={errors.description ? "error" : ""}
                  style={{ resize: 'vertical', minHeight: '100px' }}
                  maxLength={2000}
                />
                {errors.description && <span className="error-message">{errors.description}</span>}
              </div>

              <div className="form-group">
                <label htmlFor="prepTime">Время подготовки (мин)</label>
                <input
                  id="prepTime"
                  name="prepTime"
                  type="number"
                  value={formData.prepTime}
                  onChange={handleInputChange}
                  placeholder="15"
                  min="0"
                />
              </div>

              <div className="form-group">
                <label htmlFor="cookTime">Время готовки (мин)</label>
                <input
                  id="cookTime"
                  name="cookTime"
                  type="number"
                  value={formData.cookTime}
                  onChange={handleInputChange}
                  placeholder="30"
                  min="0"
                />
              </div>

              <div className="form-group">
                <label htmlFor="servings">Количество порций</label>
                <input
                  id="servings"
                  name="servings"
                  type="number"
                  value={formData.servings}
                  onChange={handleInputChange}
                  placeholder="4"
                  min="1"
                  max="100"
                />
              </div>

              <div className="form-group">
                <label htmlFor="difficulty">Сложность</label>
                <select
                  id="difficulty"
                  name="difficulty"
                  value={formData.difficulty}
                  onChange={handleInputChange}
                >
                  <option value="easy">Легко</option>
                  <option value="medium">Средне</option>
                  <option value="hard">Сложно</option>
                </select>
              </div>

              <div className="form-group">
                <label htmlFor="category">Категория</label>
                <select
                  id="category"
                  name="category"
                  value={formData.category}
                  onChange={handleInputChange}
                  disabled={loadingCategories || categories.length === 0}
                >
                  <option value="">{categories.length ? "Выберите категорию" : "Нет доступных категорий"}</option>
                  {categories.map(cat => (
                    <option key={cat.id} value={cat.id}>
                      {cat.name}
                    </option>
                  ))}
                </select>
                {loadingCategories && (
                  <small className="help-text">Загрузка категорий...</small>
                )}
                {!loadingCategories && categories.length === 0 && (
                  <small className="help-text warning">
                    Нет категорий. Создайте их в админке прежде чем сохранять рецепт.
                  </small>
                )}
              </div>

              <div className="form-group full-width">
                <label htmlFor="imageUrl">Ссылка на изображение</label>
                <input
                  id="imageUrl"
                  name="imageUrl"
                  type="url"
                  value={formData.imageUrl}
                  onChange={handleInputChange}
                  placeholder="https://example.com/image.jpg"
                  className={errors.imageUrl ? "error" : ""}
                />
                {errors.imageUrl ? (
                  <span className="error-message">{errors.imageUrl}</span>
                ) : (
                  <small className="help-text">
                    Можно добавить позже
                  </small>
                )}
              </div>
            </div>
          </section>

          {/* Ингредиенты */}
          <section className="form-section">
            <div className="section-header">
              <h2>Ингредиенты</h2>
              <button 
                type="button" 
                className="btn-add"
                onClick={addIngredient}
              >
                + Добавить ингредиент
              </button>
            </div>

            {errors.ingredients && (
              <span className="error-message section-error">{errors.ingredients}</span>
            )}

            <div className="ingredients-list">
              {ingredients.map((ingredient, index) => (
                <div key={ingredient.id} className="ingredient-row">
                  <span className="ingredient-number">{index + 1}</span>
                  
                  <input
                    type="number"
                    placeholder="Кол-во"
                    value={ingredient.amount}
                    onChange={(e) => updateIngredient(ingredient.id, { amount: e.target.value })}
                    className="ingredient-amount"
                    min="0"
                    step="0.1"
                  />
                  
                  <select
                    value={ingredient.unit}
                    onChange={(e) => updateIngredient(ingredient.id, { unit: e.target.value })}
                    className="ingredient-unit"
                  >
                    <option value="g">г</option>
                    <option value="kg">кг</option>
                    <option value="ml">мл</option>
                    <option value="l">л</option>
                    <option value="tsp">ч.л.</option>
                    <option value="tbsp">ст.л.</option>
                    <option value="cup">стакан</option>
                    <option value="piece">шт</option>
                  </select>

                  <input
                    type="number"
                    placeholder="Вес, г"
                    value={ingredient.weight || ""}
                    onChange={(e) => updateIngredient(ingredient.id, { weight: e.target.value })}
                    className="ingredient-weight"
                    min="0"
                    step="0.1"
                  />
                  
                  <input
                    type="text"
                    placeholder="Название ингредиента"
                    value={ingredient.name}
                    onChange={(e) => updateIngredient(ingredient.id, { name: e.target.value })}
                    className="ingredient-name"
                    required
                  />
                  
                  <div className="ingredient-actions">
                    <button
                      type="button"
                      className="btn-duplicate"
                      onClick={() => duplicateIngredient(ingredient.id)}
                      title="Дублировать ингредиент"
                    >
                      📋
                    </button>
                    <button
                      type="button"
                      className="btn-remove"
                      onClick={() => removeIngredient(ingredient.id)}
                      disabled={ingredients.length === 1}
                      title="Удалить ингредиент"
                    >
                      ×
                    </button>
                  </div>

                  <div className="ingredient-nutrition">
                    <div className="nutrition-inputs">
                      <input
                        type="number"
                        placeholder="Ккал"
                        value={ingredient.calories || ""}
                        onChange={(e) => updateIngredient(ingredient.id, { calories: e.target.value })}
                        className="nutrition-input"
                        min="0"
                        step="0.1"
                      />
                      <input
                        type="number"
                        placeholder="Белки, г"
                        value={ingredient.protein || ""}
                        onChange={(e) => updateIngredient(ingredient.id, { protein: e.target.value })}
                        className="nutrition-input"
                        min="0"
                        step="0.1"
                      />
                      <input
                        type="number"
                        placeholder="Жиры, г"
                        value={ingredient.fat || ""}
                        onChange={(e) => updateIngredient(ingredient.id, { fat: e.target.value })}
                        className="nutrition-input"
                        min="0"
                        step="0.1"
                      />
                      <input
                        type="number"
                        placeholder="Углеводы, г"
                        value={ingredient.carbs || ""}
                        onChange={(e) => updateIngredient(ingredient.id, { carbs: e.target.value })}
                        className="nutrition-input"
                        min="0"
                        step="0.1"
                      />
                    </div>
                    <div className="nutrition-chips">
                      <span>Ккал: {formatMacroValue(ingredient.calories)}</span>
                      <span>Б: {formatMacroValue(ingredient.protein)}</span>
                      <span>Ж: {formatMacroValue(ingredient.fat)}</span>
                      <span>У: {formatMacroValue(ingredient.carbs)}</span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </section>

          {/* Панель питания */}
          <section className="form-section nutrition-interactive-panel">
            <div className="nutrition-panel-header">
              <h2>📊 Анализ питательности</h2>
              <button
                type="button"
                className="btn-toggle-panel"
                onClick={() => setShowNutritionPanel(!showNutritionPanel)}
              >
                {showNutritionPanel ? '▼ Скрыть' : '▶ Показать'}
              </button>
            </div>

            {showNutritionPanel && (
              <div className="nutrition-panel-content">
                <div className="servings-calculator">
                  <div className="servings-control">
                    <label>Количество порций:</label>
                    <div className="servings-input-group">
                      <button
                        type="button"
                        className="btn-servings"
                        onClick={() => {
                          const current = Number(formData.servings) || 1;
                          handleInputChange({ target: { name: 'servings', value: Math.max(1, current - 1).toString() } });
                        }}
                      >
                        −
                      </button>
                      <input
                        type="number"
                        min="1"
                        max="20"
                        name="servings"
                        value={formData.servings || ''}
                        onChange={handleInputChange}
                        className="servings-input"
                      />
                      <button
                        type="button"
                        className="btn-servings"
                        onClick={() => {
                          const current = Number(formData.servings) || 1;
                          handleInputChange({ target: { name: 'servings', value: Math.min(20, current + 1).toString() } });
                        }}
                      >
                        +
                      </button>
                    </div>
                  </div>
                  <div className="servings-nutrition">
                    <div className="serving-macro">
                      <span className="macro-icon">🔥</span>
                      <div>
                        <strong>{nutritionPerServing.calories}</strong>
                        <small>ккал</small>
                      </div>
                    </div>
                    <div className="serving-macro">
                      <span className="macro-icon">💪</span>
                      <div>
                        <strong>{nutritionPerServing.protein}</strong>
                        <small>г белка</small>
                      </div>
                    </div>
                    <div className="serving-macro">
                      <span className="macro-icon">🥑</span>
                      <div>
                        <strong>{nutritionPerServing.fat}</strong>
                        <small>г жиров</small>
                      </div>
                    </div>
                    <div className="serving-macro">
                      <span className="macro-icon">🌾</span>
                      <div>
                        <strong>{nutritionPerServing.carbs}</strong>
                        <small>г углеводов</small>
                      </div>
                    </div>
                    <div className="serving-macro">
                      <span className="macro-icon">⚖️</span>
                      <div>
                        <strong>{nutritionPerServing.weight}</strong>
                        <small>г</small>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="macro-balance">
                  <h3>Баланс БЖУ</h3>
                  <div className="macro-chart">
                    <div className="macro-bar">
                      <div className="macro-bar-label">
                        <span>Белки</span>
                        <span>{nutritionAnalysis.proteinPct}%</span>
                      </div>
                      <div className="macro-bar-track">
                        <div 
                          className="macro-bar-fill protein"
                          style={{ width: `${Math.min(100, nutritionAnalysis.proteinPct)}%` }}
                        ></div>
                      </div>
                    </div>
                    <div className="macro-bar">
                      <div className="macro-bar-label">
                        <span>Жиры</span>
                        <span>{nutritionAnalysis.fatPct}%</span>
                      </div>
                      <div className="macro-bar-track">
                        <div 
                          className="macro-bar-fill fat"
                          style={{ width: `${Math.min(100, nutritionAnalysis.fatPct)}%` }}
                        ></div>
                      </div>
                    </div>
                    <div className="macro-bar">
                      <div className="macro-bar-label">
                        <span>Углеводы</span>
                        <span>{nutritionAnalysis.carbsPct}%</span>
                      </div>
                      <div className="macro-bar-track">
                        <div 
                          className="macro-bar-fill carbs"
                          style={{ width: `${Math.min(100, nutritionAnalysis.carbsPct)}%` }}
                        ></div>
                      </div>
                    </div>
                  </div>
                </div>

                {nutritionAnalysis.recommendations.length > 0 && (
                  <div className="nutrition-recommendations">
                    <h3>💡 Рекомендации</h3>
                    <div className="recommendations-list">
                      {nutritionAnalysis.recommendations.map((rec, idx) => (
                        <div key={idx} className={`recommendation-item ${rec.type}`}>
                          <span className="rec-icon">
                            {rec.type === 'success' && '✅'}
                            {rec.type === 'warning' && '⚠️'}
                            {rec.type === 'info' && 'ℹ️'}
                          </span>
                          <span>{rec.text}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                <div className="nutrition-stats">
                  <div className="stat-card">
                    <h4>На весь рецепт</h4>
                    <div className="stat-values">
                      <div><strong>{nutritionTotals.calories.toFixed(0)}</strong> ккал</div>
                      <div><strong>{nutritionTotals.weight.toFixed(0)}</strong> г</div>
                    </div>
                  </div>
                  <div className="stat-card">
                    <h4>На 100 г</h4>
                    <div className="stat-values">
                      <div><strong>{nutritionPer100.calories.toFixed(0)}</strong> ккал</div>
                      <div><strong>100</strong> г</div>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </section>

          {/* Шаги приготовления */}
          <section className="form-section">
            <div className="section-header">
              <h2>Шаги приготовления</h2>
              <button 
                type="button" 
                className="btn-add"
                onClick={addInstruction}
              >
                + Добавить шаг
              </button>
            </div>

            {errors.instructions && (
              <span className="error-message section-error">{errors.instructions}</span>
            )}

            <div className="instructions-list">
              {instructions.map((instruction, index) => (
                <div key={instruction.id} className="instruction-item">
                  <div className="instruction-header">
                    <span className="step-number">Шаг {index + 1}</span>
                    <div className="instruction-actions">
                      <button
                        type="button"
                        className="btn-duplicate"
                        onClick={() => duplicateInstruction(instruction.id)}
                        title="Дублировать шаг"
                      >
                        📋
                      </button>
                      <button
                        type="button"
                        className="btn-remove"
                        onClick={() => removeInstruction(instruction.id)}
                        disabled={instructions.length === 1}
                        title="Удалить шаг"
                      >
                        ×
                      </button>
                    </div>
                  </div>
                  <textarea
                    placeholder="Опишите этот шаг приготовления..."
                    value={instruction.text}
                    onChange={(e) => updateInstruction(instruction.id, e.target.value)}
                    rows="3"
                    required
                  />
                </div>
              ))}
            </div>
          </section>

          {/* Теги */}
          <section className="form-section">
            <h2>Теги</h2>
            <div className="tags-input-container">
              <div className="tags-input">
                <input
                  type="text"
                  placeholder="Добавьте теги (веганский, быстрый, и т.д.)"
                  value={newTag}
                  onChange={(e) => setNewTag(e.target.value)}
                  onKeyPress={handleKeyPress}
                />
                <button type="button" className="btn-add-tag" onClick={addTag}>
                  Добавить
                </button>
              </div>
              
              {tags.length > 0 && (
                <div className="tags-list">
                  {tags.map((tag, index) => (
                    <span key={index} className="tag">
                      {tag}
                      <button 
                        type="button" 
                        onClick={() => removeTag(tag)}
                        className="tag-remove"
                      >
                        ×
                      </button>
                    </span>
                  ))}
                </div>
              )}
            </div>
          </section>

          {/* Кнопки отправки */}
          <div className="form-actions">
            <button
              type="button"
              className="btn-secondary"
              onClick={() => navigate(-1)}
              disabled={loading}
            >
              Отмена
            </button>
            <button
              type="submit"
              className="btn-primary"
              disabled={loading}
            >
              {loading ? (
                <>
                  <div className="loading-spinner"></div>
                  Сохранение...
                </>
              ) : (
                "Опубликовать рецепт"
              )}
            </button>
          </div>

          {errors.submit && (
            <div className="error-banner">
              {errors.submit}
            </div>
          )}
        </form>
      </div>
    </div>
  );
}
