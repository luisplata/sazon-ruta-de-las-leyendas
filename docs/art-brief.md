# Sazón: Ruta de las Leyendas — Art Brief

Lista de ingredientes y recetas para ilustrar las cartas. Cada carta tiene un slot `art` (sprite) en el Inspector — el arte se asigna ahí, sin tocar código.

---

## 🔴 PENDIENTE — dibujos que faltan (para el dibujante)

### 1. Platos sin arte (4)
| Plato | Ingredientes |
|---|---|
| Elote con Chile | Maíz + Chile Piquín |
| Elote Picante | Maíz + Tomate + Chile Piquín |
| Arroz de Coco | Arroz + Coco |
| Pescado con Plátano | Pescado + Plátano |

### 2. Resultados de FALLO (3 sprites genéricos — hoy muestran emoji)
Cuando el jugador cocina mal, el resultado muestra un sprite genérico por tipo de fallo:
- **Comida fallida / cruda** (resultado **Fail**, color rojo) — ej. pescado crudo servido, plato desastroso
- **Plato maldito** (resultado **Cursed**, color morado) — combinación maldita de ingredientes
- **Plato insípido / relleno** (resultado **Filler**, color gris) — algo que no es receta

> El resultado **Presented** (azul) ya usa el arte de su receta; **Star/Normal** (verde/oro) usan el de sus recetas.

### 3. Personajes (hoy son cuadrados blancos placeholder)
| Personaje | Rol |
|---|---|
| **La Llorona** | Tutorial — la maestra/narradora que enseña a cocinar |
| **El Ahuizotl** | México (nivel 1) |
| **Hombre Caimán** | Colombia (nivel 2) |
| **El chef jugador** | El protagonista |

> El enemigo cambia según el nivel → se necesitan los 3 sprites (o uno por nivel).

### 4. Fondos
- **Fondo de la partida** (hoy es color sólido café) — ideal: 1 por país (México/Colombia) + tutorial
- *(Opcional)* Fondo del menú

### 5. Ambiguos (confirmar)
- `food_20_mazorcaClasico` — ¿qué plato es? (¿Elote Clásico o Elote con Chile?)
- `food_24_mazorcaAsada` — duplicado de `food_14`, ¿se usa o sobra?

---

## ✅ YA CUBIERTO

## 🃏 Ingredientes (14 cartas)

**Colores de rol**: 🔴 Base (rojo) · 🟢 Complemento (verde) · 🟡 Sazón (amarillo)

| Carta | Rol | Emoji placeholder actual | Usada en recetas |
|---|---|---|---|
| Maíz | 🔴 Base | 🌽 | Sí (7 recetas) |
| Pescado | 🔴 Base | 🐟 | Sí (6 recetas) |
| Arroz | 🟢 Complemento | 🍚 | Sí (2) |
| Cebolla | 🟢 Complemento | 🧅 | Sí (2) |
| Insectos (chapulines) | 🟢 Complemento | 🦗 | Sí (1 — tacos) |
| Plátano | 🟢 Complemento | 🍌 | Sí (2) |
| Tomate | 🟢 Complemento | 🍅 | Sí (2) |
| Yuca | 🟢 Complemento | 🥔 | No (aún) |
| Ajo | 🟡 Sazón | 🧄 | Sí (1) |
| Chile Piquín | 🟡 Sazón | 🌶️ | Sí (3) |
| Cilantro | 🟡 Sazón | 🌿 | Sí (1) |
| Coco | 🟡 Sazón | 🥥 | Sí (2) |
| Limón | 🟡 Sazón | 🍋 | Sí (1) |
| Sal | 🟡 Sazón | 🧂 | No (aún) |

## 🍲 Recetas (12)

**⭐ = estrella del nivel · N = normal**

| Plato | Tipo | Ingredientes | Nivel |
|---|---|---|---|
| **Tacos de Chapulines** 🌮 | ⭐ Star | Maíz + Insectos + Chile Piquín | México (y tutorial — lo pide La Llorona) |
| **Viudo de Pescado** 🐟 | ⭐ Star | Pescado + Plátano + Ajo | Colombia |
| Elote Asado 🌽 | N | Maíz | México |
| Elote con Chile 🌶️ | N | Maíz + Chile Piquín | México |
| Elote Picante 🌶️ | N | Maíz + Tomate + Chile Piquín | México |
| Esquites Clásicos 🌽 | N | Maíz + Cebolla + Limón | México |
| Esquites Sencillos 🥣 | N | Maíz + Cebolla | México |
| Arroz de Coco 🍚 | N | Arroz + Coco | Colombia |
| Arroz de Coco con Pescado 🍚 | N | Arroz + Coco + Pescado | Colombia |
| Pescado Crudo 🐟 | N | Pescado | Colombia |
| Pescado con Plátano 🍌 | N | Pescado + Plátano | Colombia |
| Sancocho de Pescado 🍲 | N | Pescado + Tomate + Cilantro | Colombia |

## 🎨 Notas

- **Regla de roles**: cada receta tiene máximo 1 Base + 1 Complemento + 1 Sazón (nunca 2 del mismo rol).
- **Yuca y Sal** están en el mazo pero sin receta todavía (en revisión).
- El emoji actual es el placeholder que se muestra en el juego hoy; el arte lo reemplaza.
- Estilo: comida mexicana y colombiana con aire de leyenda (El Ahuizotl, La Llorona, Hombre Caimán).
- **Tamaño sugerido**: sprite cuadrado (la carta usa ~105×103 px en pantalla).