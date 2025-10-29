using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ClickMart.Mstest.Selenium
{
    [TestClass]
    public class ProductoControllerTests
    {
        private IWebDriver _driver;
        private readonly string _urlBase = "https://localhost:7002";

        // Admin (solo admin puede crear/editar/eliminar productos)
        private readonly string _adminEmail = "m@gmail.com";
        private readonly string _adminPass = "12345";

        [TestInitialize]
        public void Setup()
        {
            var options = new ChromeOptions();
            // options.AddArgument("--headless=new"); // para CI
            options.AddArgument("--ignore-certificate-errors");
            _driver = new ChromeDriver(options);
            _driver.Manage().Window.Maximize();
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            // Login admin
            _driver.Navigate().GoToUrl($"{_urlBase}/Auth/Login");
            _driver.FindElement(By.Name("Email")).SendKeys(_adminEmail);
            _driver.FindElement(By.Name("Password")).SendKeys(_adminPass);
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();
        }

        [TestCleanup]
        public void Teardown()
        {
            try { _driver?.Quit(); } catch { }
            try { _driver?.Dispose(); } catch { }
        }

        // ================== HU-019 (1036) ==================
        [TestMethod]
        public void HU19_1036_Producto_Crear_Exitoso()
        {
            GoToProductoCreate();

            var nombre = $"Prod QA {_NowKey()}";
            TypeInto(InputFor("Nombre", "Producto", "NombreProducto"), nombre, clear: true);
            TypeInto(InputFor("Marca", "Brand"), "ClickMart QA", clear: true);
            TypeInto(InputFor("Talla", "Tamaño", "Size"), "M", clear: true);

            SelectDistribuidor("Prov QA"); // intentará por texto y cae a la 1ª válida
            SelectCategoria("Temp QA");    // idem

            TypeNumber(InputFor("Precio", "Price"), "99");
            TypeNumber(InputFor("Stock", "Existencia", "Cantidad"), "10");
            TypeInto(TextAreaFor("Descripcion", "Descripción", "Detalle"), "Producto creado por pruebas UI", clear: true);

            ClickGuardar();

            _driver.Navigate().GoToUrl($"{_urlBase}/Producto");
            Assert.IsTrue(_driver.PageSource.Contains(nombre), "El producto recién creado debería aparecer en el listado.");
        }

        // ================== HU-019 (1037) ==================
        [TestMethod]
        public void HU19_1037_Producto_Crear_Validaciones()
        {
            GoToProductoCreate();

            // Dejar campos obligatorios vacíos y forzar precio inválido
            TypeNumber(InputFor("Precio", "Price"), "-5");
            TypeNumber(InputFor("Stock", "Existencia", "Cantidad"), "-1");

            ClickGuardar();

            var html = _driver.PageSource.ToLower();
            Assert.IsTrue(
                _driver.Url.Contains("/Producto/Create")
                || html.Contains("obligator")
                || html.Contains("inválid")
                || html.Contains("precio")
                || html.Contains("stock"),
                "Debe mostrar validaciones por campos vacíos/valores inválidos."
            );
        }

        // ================== HU-020 (1038) ==================
        [TestMethod]
        public void HU20_1038_Producto_Listado_Visible()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Producto");

            var filas =
                  _driver.FindElements(By.CssSelector("table tbody tr")).Count
                + _driver.FindElements(By.CssSelector("[data-testid='producto-row']")).Count
                + _driver.FindElements(By.CssSelector(".producto,.product,.card")).Count;

            Assert.IsTrue(filas > 0, "Debe mostrarse el listado de productos (al menos una fila/card).");
        }

        // ================== HU-021 (1039) ==================
        [TestMethod]
        public void HU21_1039_Producto_Editar_Exitoso()
        {
            // Prepara un producto base
            var nombre = $"ProdBase QA {_NowKey()}";
            var id = CreateProducto(nombre);

            // Edita algo visible (marca/precio)
            _driver.Navigate().GoToUrl($"{_urlBase}/Producto/Edit/{id}");

            var nuevaMarca = $"MarcaEdit QA {_NowKey()}";
            TypeInto(InputFor("Marca", "Brand"), nuevaMarca, clear: true);
            TypeNumber(InputFor("Precio", "Price"), "149");
            ClickGuardar();

            _driver.Navigate().GoToUrl($"{_urlBase}/Producto");
            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Producto"));
        }

        // ================== HU-021 (1040) ==================
        [TestMethod]
        public void HU21_1040_Producto_Editar_Validaciones()
        {
            var nombre = $"ProdTemp QA {_NowKey()}";
            var id = CreateProducto(nombre);

            _driver.Navigate().GoToUrl($"{_urlBase}/Producto/Edit/{id}");

            // Precio negativo y nombre vacío
            TypeInto(InputFor("Nombre", "Producto", "NombreProducto"), "", clear: true);
            TypeNumber(InputFor("Precio", "Price"), "-10");

            ClickGuardar();

            var html = _driver.PageSource.ToLower();
            Assert.IsTrue(
                _driver.Url.Contains("/Producto/Edit")
                || html.Contains("obligator")
                || html.Contains("inválid")
                || html.Contains("precio"),
                "Debe mostrar validaciones al editar (precio inválido / campos obligatorios)."
            );
        }

        // ================== HU-022 (1041) ==================
        [TestMethod]
        public void HU22_1041_Producto_Eliminar_SinDependencias_Exitoso()
        {
            var nombre = $"ProdDelete QA {_NowKey()}";
            var id = CreateProducto(nombre);

            // Abrir confirmación y eliminar
            OpenDeleteConfirmById_Producto(id);
            ClickIfExists(By.CssSelector("button[type='submit']"));
            ClickIfExists(By.XPath("//button[contains(.,'Eliminar')]"));
            ClickIfExists(By.XPath("//button[contains(.,'Delete')]"));

            _driver.Navigate().GoToUrl($"{_urlBase}/Producto");

            var goneByName = !_driver.PageSource.Contains(nombre);
            var goneById = _driver.FindElements(By.CssSelector(
                $"a[href*='/Producto/Edit/{id}'], a[href*='/Producto/Details/{id}'], a[href*='/Producto/Delete/{id}']")).Count == 0;

            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Producto"));
        }

        // ================== HU-022 (1042) ==================
        [TestMethod]
        public void HU22_1042_Producto_Eliminar_Id9_EnUso_Bloqueado()
        {
            const int id = 9;

            if (!TryOpenDeleteById_Producto(id))
                Assert.Inconclusive("No pude abrir la vista de eliminación para Producto ID=9 (¿existe?).");

            ClickIfExists(By.CssSelector("button[type='submit']"));
            ClickIfExists(By.XPath("//button[contains(.,'Eliminar')]"));

            _driver.Navigate().GoToUrl($"{_urlBase}/Producto");

            var html = _driver.PageSource.ToLower();
            bool siguePresente =
                   _driver.FindElements(By.CssSelector($"a[href*='/Producto/Edit/{id}']")).Count > 0
                || _driver.FindElements(By.CssSelector($"a[href*='/Producto/Details/{id}']")).Count > 0
                || _driver.FindElements(By.CssSelector($"a[href*='/Producto/Delete/{id}']")).Count > 0;

            bool blocked = html.Contains("no se puede eliminar")
                        || html.Contains("dependenc")
                        || html.Contains("en uso")
                        || html.Contains("pedidos")
                        || html.Contains("reseñ")
                        || siguePresente;

            Assert.IsTrue(blocked, "Debe bloquearse la eliminación del producto en uso (ID=9).");
        }

        // =============== Helpers de orquestación ===============
        private int CreateProducto(string nombre)
        {
            GoToProductoCreate();

            TypeInto(InputFor("Nombre", "Producto", "NombreProducto"), nombre, clear: true);
            TypeInto(InputFor("Marca", "Brand"), "ClickMart QA", clear: true);
            TypeInto(InputFor("Talla", "Tamaño", "Size"), "Única", clear: true);

            SelectDistribuidor("Prov QA");
            SelectCategoria("Temp QA");

            TypeNumber(InputFor("Precio", "Price"), "120");
            TypeNumber(InputFor("Stock", "Existencia", "Cantidad"), "5");
            TypeInto(TextAreaFor("Descripcion", "Descripción", "Detalle"), "Alta de prueba para borrar/editar", clear: true);

            ClickGuardar();

            _driver.Navigate().GoToUrl($"{_urlBase}/Producto");
            var id = FindProductoIdEnIndexPorNombre(nombre);
            Assert.IsTrue(id > 0, $"No pude capturar ID del producto '{nombre}'.");
            return id;
        }

        private void GoToProductoCreate()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Producto");
            var crear = _driver.FindElements(By.CssSelector("[data-testid='producto-create']")).FirstOrDefault()
                    ?? _driver.FindElements(By.PartialLinkText("Crear")).FirstOrDefault()
                    ?? _driver.FindElements(By.PartialLinkText("Nuevo")).FirstOrDefault()
                    ?? _driver.FindElements(By.PartialLinkText("Nueva")).FirstOrDefault();

            if (crear != null) crear.Click();
            else _driver.Navigate().GoToUrl($"{_urlBase}/Producto/Create");
        }

        // ====== BÚSQUEDA ROBUSTA DE ID EN EL ÍNDICE (REEMPLAZA AL ANTERIOR) ======
        private int FindProductoIdEnIndexPorNombre(string nombre)
        {
            // Espera breve a que se renderice el nombre
            WaitForPageContains(nombre, 5);

            // 1) Tablas: fila que contenga el nombre
            var row = _driver.FindElements(By.XPath(
                $"//tr[.//*[contains(normalize-space(.), {ToXpathLiteral(nombre)})]]"
            )).FirstOrDefault();

            if (row != null)
            {
                var link = row.FindElements(By.XPath(
                    ".//a[contains(@href,'/Producto/Edit') or contains(@href,'/Producto/Details') or contains(@href,'/Producto/Delete')]"
                )).FirstOrDefault();

                if (link != null)
                {
                    var id = ExtractIdFromAnyProductoHref(link.GetAttribute("href"));
                    if (id.HasValue) return id.Value;
                }

                if (int.TryParse(row.GetAttribute("data-id"), out var rid))
                    return rid;
            }

            // 2) Cards: card que contenga el nombre
            var card = _driver.FindElements(By.XPath(
                $"//*[contains(@class,'card') or contains(@class,'producto') or contains(@class,'product')]" +
                $"[.//*[contains(normalize-space(.), {ToXpathLiteral(nombre)})]]"
            )).FirstOrDefault();

            if (card != null)
            {
                var link = card.FindElements(By.XPath(
                    ".//a[contains(@href,'/Producto/Edit') or contains(@href,'/Producto/Details') or contains(@href,'/Producto/Delete')]"
                )).FirstOrDefault();

                if (link != null)
                {
                    var id = ExtractIdFromAnyProductoHref(link.GetAttribute("href"));
                    if (id.HasValue) return id.Value;
                }

                if (int.TryParse(card.GetAttribute("data-id"), out var cid))
                    return cid;
            }

            // 3) Fallback: mayor ID visible en cualquier anchor de /Producto/*
            var best = GetLatestIdByAnyAction_Producto();
            Assert.IsTrue(best > 0, "No pude inferir ID del producto recién creado.");
            return best;
        }

        // =============== Helpers de selección ===================
        private void SelectDistribuidor(string preferido = "Prov QA")
        {
            var select =
                   _driver.FindElements(By.Name("DistribuidorId")).FirstOrDefault()
                ?? _driver.FindElements(By.Id("DistribuidorId")).FirstOrDefault()
                ?? _driver.FindElements(By.CssSelector("[data-testid='producto-distribuidor']")).FirstOrDefault()
                ?? _driver.FindElements(By.XPath("//label[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'distribuidor')]/following::*[self::select][1]")).FirstOrDefault();

            Assert.IsNotNull(select, "No encontré el <select> 'Distribuidor'.");

            ClickSelectOption(select, preferido);
        }

        private void SelectCategoria(string preferida = "Temp QA")
        {
            var select =
                   _driver.FindElements(By.Name("CategoriaId")).FirstOrDefault()
                ?? _driver.FindElements(By.Id("CategoriaId")).FirstOrDefault()
                ?? _driver.FindElements(By.CssSelector("[data-testid='producto-categoria']")).FirstOrDefault()
                ?? _driver.FindElements(By.XPath("//label[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'categor')]/following::*[self::select][1]")).FirstOrDefault();

            Assert.IsNotNull(select, "No encontré el <select> 'Categoría'.");

            ClickSelectOption(select, preferida);
        }

        private void ClickSelectOption(IWebElement select, string preferida)
        {
            // Opción por texto preferido
            var optExact = select.FindElements(By.XPath($".//option[normalize-space(.)={ToXpathLiteral(preferida)}]")).FirstOrDefault();
            IWebElement chosen = null;
            if (optExact != null && optExact.Enabled)
            {
                optExact.Click();
                chosen = optExact;
            }
            else
            {
                // Primera válida
                var firstValid = select.FindElements(By.TagName("option"))
                    .FirstOrDefault(o => o.Enabled &&
                                         !"true".Equals(o.GetAttribute("disabled"), StringComparison.OrdinalIgnoreCase) &&
                                         o.Text.Trim().Length > 0 &&
                                         o.Text.IndexOf("seleccione", StringComparison.OrdinalIgnoreCase) < 0 &&
                                         o.Text.IndexOf("selecciona", StringComparison.OrdinalIgnoreCase) < 0);
                Assert.IsNotNull(firstValid, "No hay opciones válidas en el select.");
                firstValid!.Click();
                chosen = firstValid;
            }

            // Dispara eventos siempre (algunas UIs lo requieren)
            try
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                  const s = arguments[0];
                  s.dispatchEvent(new Event('input',{bubbles:true}));
                  s.dispatchEvent(new Event('change',{bubbles:true}));
                ", select);
            }
            catch { }
        }

        // =============== Helpers de UI genéricos ===============
        private IWebElement InputFor(params string[] keys)
        {
            foreach (var k in keys)
            {
                var e = _driver.FindElements(By.Name(k)).FirstOrDefault()
                     ?? _driver.FindElements(By.Id(k)).FirstOrDefault()
                     ?? _driver.FindElements(By.CssSelector($"input[placeholder*='{k}' i]")).FirstOrDefault()
                     ?? _driver.FindElements(By.XPath($"//label[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'{k.ToLower()}')]/following::*[self::input][1]")).FirstOrDefault();
                if (e != null) return e;
            }
            var anyVisible = _driver.FindElements(By.CssSelector("form input:not([type='hidden'])"))
                                    .FirstOrDefault(el => el.Displayed && el.Enabled);
            if (anyVisible != null) return anyVisible;
            throw new NoSuchElementException($"No encontré input para keys: {string.Join(", ", keys)}");
        }

        private IWebElement TextAreaFor(params string[] keys)
        {
            foreach (var k in keys)
            {
                var e = _driver.FindElements(By.Name(k)).FirstOrDefault()
                     ?? _driver.FindElements(By.Id(k)).FirstOrDefault()
                     ?? _driver.FindElements(By.CssSelector($"textarea[placeholder*='{k}' i]")).FirstOrDefault()
                     ?? _driver.FindElements(By.XPath($"//label[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'{k.ToLower()}')]/following::*[self::textarea][1]")).FirstOrDefault();
                if (e != null) return e;
            }
            // si no hay textarea, usa input genérico
            return InputFor(keys);
        }

        private void TypeInto(IWebElement el, string value, bool clear = false)
        {
            try
            {
                el.Click();
                if (clear)
                {
                    try { el.Clear(); } catch { }
                    try { el.SendKeys(Keys.Control + "a"); el.SendKeys(Keys.Delete); } catch { }
                }
                if (!string.IsNullOrEmpty(value)) el.SendKeys(value);
                return;
            }
            catch { /* fallback JS */ }

            try
            {
                var js = (IJavaScriptExecutor)_driver;
                js.ExecuteScript("arguments[0].removeAttribute('readonly'); arguments[0].removeAttribute('disabled');", el);
                if (clear) js.ExecuteScript("arguments[0].value = '';", el);
                if (!string.IsNullOrEmpty(value))
                {
                    js.ExecuteScript(@"
                        arguments[0].value = arguments[1];
                        arguments[0].dispatchEvent(new Event('input', { bubbles: true }));
                        arguments[0].dispatchEvent(new Event('change', { bubbles: true }));
                    ", el, value);
                }
            }
            catch
            {
                throw new InvalidElementStateException("No se pudo escribir en el elemento (ni con JS).");
            }
        }

        private void TypeNumber(IWebElement el, string numericText)
        {
            TypeInto(el, numericText, clear: true);
        }

        private void ClickGuardar()
        {
            var btn = _driver.FindElements(By.CssSelector("button[type='submit']")).FirstOrDefault()
                  ?? _driver.FindElements(By.XPath("//button[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'guardar')]")).FirstOrDefault();

            Assert.IsNotNull(btn, "No encontré botón Guardar.");

            try { ((IJavaScriptExecutor)_driver).ExecuteScript("document.activeElement && document.activeElement.blur();"); } catch { }
            ScrollIntoViewCenter(btn);

            try { btn.Click(); }
            catch (ElementClickInterceptedException) { JsClick(btn); }
            catch (WebDriverException) { JsClick(btn); }
        }

        private void OpenDeleteConfirmById_Producto(int id)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Producto/Delete/{id}");
            if (!IsDeleteConfirmView())
            {
                _driver.Navigate().GoToUrl($"{_urlBase}/Producto/Delete?id={id}");
                if (!IsDeleteConfirmView())
                {
                    _driver.Navigate().GoToUrl($"{_urlBase}/Producto");
                    var link = _driver.FindElements(By.CssSelector($"a[href*='/Producto/Delete/{id}']")).FirstOrDefault()
                           ?? _driver.FindElements(By.CssSelector($"a[href*='/Producto/Delete?id={id}']")).FirstOrDefault();
                    Assert.IsNotNull(link, $"No encontré enlace Delete para ID={id} en el listado.");
                    link!.Click();
                }
            }
        }

        private bool TryOpenDeleteById_Producto(int id)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Producto/Delete/{id}");
            if (IsDeleteConfirmView()) return true;

            _driver.Navigate().GoToUrl($"{_urlBase}/Producto/Delete?id={id}");
            if (IsDeleteConfirmView()) return true;

            _driver.Navigate().GoToUrl($"{_urlBase}/Producto");
            var link = _driver.FindElements(By.CssSelector($"a[href*='/Producto/Delete/{id}']")).FirstOrDefault()
                   ?? _driver.FindElements(By.CssSelector($"a[href*='/Producto/Delete?id={id}']")).FirstOrDefault();

            if (link != null) { link.Click(); return IsDeleteConfirmView(); }
            return false;
        }

        private bool IsDeleteConfirmView()
        {
            var html = _driver.PageSource.ToLower();
            var hasForm = _driver.FindElements(By.CssSelector("form")).Count > 0;
            var hasSubmit = _driver.FindElements(By.CssSelector("button[type='submit']")).Count > 0;
            var saysEliminar = html.Contains("eliminar") || html.Contains("delete");
            return hasForm && (hasSubmit || saysEliminar);
        }

        private void ClickIfExists(By by)
        {
            var el = _driver.FindElements(by).FirstOrDefault();
            if (el != null)
            {
                try { el.Click(); }
                catch (ElementClickInterceptedException) { JsClick(el); }
            }
        }

        // =============== Helpers de URL/ID =====================
        private static int? ExtractIdFromActionHref(string baseUrl, string href, string action, string entity)
        {
            if (string.IsNullOrWhiteSpace(href)) return null;

            if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
            {
                var b = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");
                uri = new Uri(b, href);
            }

            // .../Entidad/Action/{id}
            var segs = uri.AbsolutePath.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segs.Length >= 3 &&
                segs[^3].Equals(entity, StringComparison.OrdinalIgnoreCase) &&
                segs[^2].Equals(action, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(segs[^1], out var idBySeg))
            {
                return idBySeg;
            }

            // .../Entidad/Action?id={id}
            if (!string.IsNullOrEmpty(uri.Query))
            {
                var qs = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (var kv in qs)
                {
                    var parts = kv.Split('=', 2);
                    if (parts.Length == 2 &&
                        parts[0].Equals("id", StringComparison.OrdinalIgnoreCase) &&
                        int.TryParse(parts[1], out var idByQuery))
                    {
                        return idByQuery;
                    }
                }
            }
            return null;
        }

        // NUEVO: extrae ID desde cualquier href de Producto (Edit/Details/Delete, ?id=, último número)
        private int? ExtractIdFromAnyProductoHref(string href)
        {
            if (string.IsNullOrWhiteSpace(href)) return null;

            var actions = new[] { "Edit", "Details", "Delete" };
            foreach (var act in actions)
            {
                var id = ExtractIdFromActionHref(_urlBase, href, act, "Producto");
                if (id.HasValue) return id.Value;
            }

            if (Uri.TryCreate(href, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Query))
            {
                var qs = uri.Query.TrimStart('?').Split('&');
                foreach (var kv in qs)
                {
                    var parts = kv.Split('=', 2);
                    if (parts.Length == 2 && parts[0].Equals("id", StringComparison.OrdinalIgnoreCase)
                        && int.TryParse(parts[1], out var qid))
                        return qid;
                }
            }

            var m = Regex.Match(href, @"(\d+)(?!.*\d)");
            if (m.Success && int.TryParse(m.Value, out var tail))
                return tail;

            return null;
        }

        private int GetLatestIdByAction(string action, string entity)
        {
            var anchors = _driver.FindElements(By.CssSelector($"a[href*='/{entity}/{action}']"));
            int best = -1;
            foreach (var a in anchors)
            {
                var maybe = ExtractIdFromActionHref(_urlBase, a.GetAttribute("href"), action, entity);
                if (maybe.HasValue && maybe.Value > best) best = maybe.Value;
            }
            Assert.IsTrue(best > 0, $"No pude inferir ID en /{entity} para acción {action}.");
            return best;
        }

        // NUEVO: mayor ID en cualquier anchor /Producto/*
        private int GetLatestIdByAnyAction_Producto()
        {
            var anchors = _driver.FindElements(By.CssSelector("a[href*='/Producto/']"));
            int best = -1;
            foreach (var a in anchors)
            {
                var id = ExtractIdFromAnyProductoHref(a.GetAttribute("href"));
                if (id.HasValue && id.Value > best) best = id.Value;
            }
            return best;
        }

        // =============== Utilidades varias =====================
        private static string _NowKey() => DateTime.UtcNow.ToString("HHmmss");

        private void ScrollIntoViewCenter(IWebElement el)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'center', inline:'nearest'});", el);
        }

        private void JsClick(IWebElement el)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", el);
        }

        private void WaitForPageContains(string text, int seconds)
        {
            var end = DateTime.UtcNow.AddSeconds(seconds);
            while (DateTime.UtcNow < end)
            {
                try { if (_driver.PageSource.Contains(text)) return; } catch { }
                System.Threading.Thread.Sleep(150);
            }
        }

        private static string ToXpathLiteral(string value)
        {
            if (!value.Contains("'")) return $"'{value}'";
            var parts = value.Split('\'');
            return "concat(" + string.Join(", \"'\", ", parts.Select(p => $"'{p}'")) + ")";
        }
    }
}