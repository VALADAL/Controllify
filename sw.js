// Service worker mínimo: no guarda nada en caché (así siempre cargas la versión más nueva).
// Existe solo para que el navegador ofrezca instalar la app.
self.addEventListener("fetch", function () {});
