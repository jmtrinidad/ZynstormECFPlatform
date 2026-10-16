# Permisos por página y búsqueda de clientes en usuarios

Fecha: 2026-10-16. Réplica del modelo de EasyInvoice (`user-permissions-modal.tsx`, `UserPageAccess`).

## 1. Búsqueda en "Asignar Clientes"

`app/configuraciones/page.tsx`: input con lupa fijo arriba de la lista del popover. Filtra por nombre
(sin mayúsculas ni acentos); muestra "Sin resultados" si no hay coincidencias; se limpia al cerrar el
popover. Los clientes marcados siguen marcados aunque el filtro los oculte.

## 2. Reglas de acceso

- SA y Admin: acceso total, no se les persiste restricción.
- Roles restringidos: cualquier otro (Normal; Cajero en el frontend).
- Páginas SA-only (`/certificacion`, `/consumo`, `/pagos`, `/configuraciones/planes`,
  `/configuraciones/seriales`): solo SA, no asignables. El guard las bloquea por URL además de ocultarlas.
- Siempre permitidas: `/` y `/configuraciones/cuenta`.
- Catálogo asignable: Clientes (`/clientes`), Facturas (`/facturas`), Configuraciones (`/configuraciones`).
- **Abierto por defecto**: un usuario restringido que nunca tuvo permisos guardados
  (`User.PageAccessConfigured = false`) ve todo el catálogo. Al guardar permisos una vez, la lista manda,
  aunque esté vacía (solo Dashboard y Cuenta).

## 3. API

- Entidad `UserPageAccess` (`UserPageAccessId`, `UserId`, `PagePath`), índice único (UserId, PagePath),
  cascade al borrar el usuario.
- `User.HomePagePath` (string?, null = Dashboard) y `User.PageAccessConfigured` (bool).
- Migración `AddUserPageAccess`.
- `IUserPageAccessManager` (Services/Users): `GetAsync(user)` devuelve `null` si no está configurado;
  `SaveAsync(user, roleName, paths, homePagePath)` normaliza (trim, sin vacíos, sin duplicados), solo
  persiste filas para roles restringidos y marca `PageAccessConfigured`.
- `UserController`:
  - `GET v1/User/page-access?userId=` → `{ pageAccess: string[] | null, homePagePath }`.
  - `PUT v1/User/page-access?userId=` body `{ pageAccess, homePagePath }`.
  - Ambos `SA,Admin`; Admin solo sobre usuarios que creó (como el listado).
  - `GET v1/User` incluye `homePagePath`.
- Login (`signIn`, `login-2fa`): `user.pageAccess` y `user.homePagePath`. Los cambios de permisos aplican
  en el siguiente login.

## 4. Frontend

- `constants/page-access.ts`: catálogo, `SA_ONLY_PATHS`, `ALWAYS_ALLOWED_PATHS`, `canAccessPage(user, path)`.
- `types/auth.type.ts`: `pageAccess?: string[] | null`, `homePagePath?: string | null`.
- `auth.service.ts` los copia del login.
- `AuthGuard`: usa `canAccessPage` y redirige a `/`. Reemplaza los checks sueltos de SA.
- `sidebar.tsx`: filtra con `canAccessPage`.
- Login: tras autenticarse va a `homePagePath` si tiene acceso; si no, a `/`.
- `UserPermissionsModal` en `app/configuraciones/components/`: selector de página de inicio y
  checkboxes por grupo con "marcar todo", mismo layout que EasyInvoice. Se abre desde el menú de
  acciones del usuario ("Permisos").

## 5. Fuera de alcance

Secciones App móvil, POS y Acciones de EasyInvoice. El acceso por cliente (`UserClient`) no cambia.

## 6. Pruebas

- xUnit del manager: rol SA/Admin no guarda filas; normalización; marca configurado; `GetAsync` null
  sin configurar.
- Frontend sin runner de pruebas: `tsc` + lint, y verificación manual (Normal con solo Clientes →
  `/facturas` redirige a `/`).
