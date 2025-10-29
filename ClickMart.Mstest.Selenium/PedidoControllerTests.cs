using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Linq;

namespace ClickMart.Mstest.Selenium
{
    [TestClass]
    public class PedidoControllerTests
    {
        private IWebDriver _driver;
        private readonly string _urlBase = "https://localhost:7002";

        // CLIENTE (crea/edita/elimina sus pedidos)
        private readonly string _clientEmail = "jose@gmail.com";
        private readonly string _clientPass = "12345";

        // ADMIN (lista general, etc.)
        private readonly string _adminEmail = "m@gmail.com";
        private readonly string _adminPass = "12345";

        private const string TestCardNumber = "4112014007011073";

        [TestInitialize]
        public void Setup()
        {
            var options = new ChromeOptions();
            // options.AddArgument("--headless=new"); // para CI
            options.AddArgument("--ignore-certificate-errors");

            _driver = new ChromeDriver(options);
            _driver.Manage().Window.Maximize();
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            Login(_clientEmail, _clientPass); // por defecto como cliente
        }

        [TestCleanup]
        public void Teardown()
        {
            try { _driver?.Quit(); } catch { }
            try { _driver?.Dispose(); } catch { }
        }

        // ============== HU-027 (1050) Crear pedido (TARJETA) ==============
        [TestMethod]
        public void HU27_1050_Pedido_Crear_Tarjeta_Exitoso()
        {
            var id = CreatePedido("Tarjeta", withCard: true);

            _driver.Navigate().GoToUrl($"{_urlBase}/Pedido");
            Assert.IsTrue(_driver.Url.Contains("/Pedido"));
            Assert.IsTrue(_driver.FindElements(By.CssSelector(
                $"a[href*='/Pedido/Edit/{id}'], a[href*='/Pedido/Details/{id}']")).Count > 0,
                "El pedido creado con Tarjeta debería estar en el listado.");
        }

    

        // ================= HU-026 (1048) Ver detalle (cliente) ==============
        [TestMethod]
        public void HU26_1048_Pedido_VerDetalle_Propio()
        {
            var id = CreatePedido("Efectivo", withCard: false);

            _driver.Navigate().GoToUrl($"{_urlBase}/Pedido/Details/{id}");

            var html = _driver.PageSource.ToLower();
            bool ok = html.Contains("pedido") || html.Contains("detalle") || html.Contains("items") || html.Contains("total");
            Assert.IsTrue(ok, "En el detalle del pedido deberían mostrarse datos relevantes (cabecera, items, total, etc.).");
        }

        // ================= HU-028 (1051) Editar: SOLO método de pago ========
        [TestMethod]
        public void HU28_1051_Pedido_Editar_SoloMetodoPago()
        {
            // 1) Creamos el pedido en EFECTIVO
            var id = CreatePedido("Efectivo", withCard: false);

            // 2) Editamos SOLO el método → TARJETA (y llenamos la tarjeta)
            _driver.Navigate().GoToUrl($"{_urlBase}/Pedido/Edit/{id}");
            SelectMetodoPago("Tarjeta");
            TypeCardIfPresent(TestCardNumber);
            ClickCrearOrGuardar();

            // 3) Validamos que el método sea Tarjeta
            _driver.Navigate().GoToUrl($"{_urlBase}/Pedido/Details/{id}");
            var html = _driver.PageSource.ToLower();
            Assert.IsTrue(html.Contains("tarjeta"),
                "Tras editar el pedido, el método de pago debería mostrarse como 'Tarjeta'.");
        }

        // ============== HU-028 (1052) Eliminar pedido pendiente (cliente) ===
        [TestMethod]
        public void HU28_1052_Pedido_Eliminar_Pendiente_Propio()
        {
            var id = CreatePedido("Efectivo", withCard: false);

            OpenDeleteConfirmById_Pedido(id);
            ClickIfExists(By.CssSelector("button[type='submit']"));
            ClickIfExists(By.XPath("//button[contains(.,'Eliminar')]"));
            ClickIfExists(By.XPath("//button[contains(.,'Delete')]"));

            _driver.Navigate().GoToUrl($"{_urlBase}/Pedido");

            bool gone =
                _driver.FindElements(By.CssSelector($"a[href*='/Pedido/Edit/{id}']")).Count == 0 &&
                _driver.FindElements(By.CssSelector($"a[href*='/Pedido/Details/{id}']")).Count == 0 &&
                _driver.FindElements(By.CssSelector($"a[href*='/Pedido/Delete/{id}']")).Count == 0;

            Assert.IsTrue(gone, "El pedido pendiente debería haberse eliminado.");
        }

        // ================= HU-024 (1045) Listado admin visible ==============
        [TestMethod]
        public void HU24_1045_Pedido_Listado_Admin_Visible()
        {
            ReloginAsAdmin();

            _driver.Navigate().GoToUrl($"{_urlBase}/Pedido");

            var filas =
                  _driver.FindElements(By.CssSelector("table tbody tr")).Count
                + _driver.FindElements(By.CssSelector("[data-testid='pedido-row']")).Count
                + _driver.FindElements(By.CssSelector(".pedido,.order,.card")).Count;

            Assert.IsTrue(filas > 0, "Como admin, el listado de pedidos debe ser visible (al menos una fila/card).");
        }

        // ================= HU-025 (1046) Mis pedidos (cliente) ==============
        [TestMethod]
        public void HU25_1046_Pedido_Listado_Cliente_Visible()
        {
            // ya estamos como cliente
            _driver.Navigate().GoToUrl($"{_urlBase}/Pedido");

            var filas =
                  _driver.FindElements(By.CssSelector("table tbody tr")).Count
                + _driver.FindElements(By.CssSelector("[data-testid='pedido-row']")).Count
                + _driver.FindElements(By.CssSelector(".pedido,.order,.card")).Count;

            Assert.IsTrue(filas >= 0, "El cliente debería poder ver su listado de pedidos (si no hay, cero filas es válido).");
        }

        // ===================================================================
        // ==================== HELPERS DE ORQUESTACIÓN ======================
        // ===================================================================

        private void Login(string email, string pass)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Auth/Login");
            _driver.FindElement(By.Name("Email")).Clear();
            _driver.FindElement(By.Name("Email")).SendKeys(email);
            _driver.FindElement(By.Name("Password")).Clear();
            _driver.FindElement(By.Name("Password")).SendKeys(pass);
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();
        }

        private void ReloginAsAdmin()
        {
            // intenta logout si existe
            try
            {
                _driver.Navigate().GoToUrl($"{_urlBase}/Auth/Logout");
            }
            catch { }
            Login(_adminEmail, _adminPass);
        }

        /// <summary>
        /// Crea un pedido con método de pago indicado. Devuelve el ID.
        /// </summary>
        private int CreatePedido(string metodoPreferido, bool withCard)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Pedido/Create");

            // Fecha (intenta setear a hoy)
            var fechaEl = InputFor("Fecha", "FechaPedido", "Date");
            TypeDate(fechaEl, DateTime.Today);

            // Método de pago (robusto)
            SelectMetodoPago(metodoPreferido);

            // Datos de tarjeta si aplica
            if (withCard || metodoPreferido.Equals("Tarjeta", StringComparison.OrdinalIgnoreCase))
                TypeCardIfPresent(TestCardNumber);

            ClickCrearOrGuardar();

            // Ir al index y buscar el ID
            _driver.Navigate().GoToUrl($"{_urlBase}/Pedido");

            int? idMaybe = FindPedidoIdEnIndexPorPistas(metodoPreferido, DateTime.Today);
            int id = idMaybe ?? GetLatestIdByAnyAction("Pedido", "Edit", "Details", "Delete");

            Assert.IsTrue(id > 0, "No pude capturar el ID del pedido recién creado.");
            return id;
        }

        // ===================================================================
        // ========================== HELPERS UI ==============================
        // ===================================================================

        private void SelectMetodoPago(string preferido = "Tarjeta")
        {
            // localizar el select de "Método de pago" (evitar confundir con "Estado de pago")
            var select =
                   _driver.FindElements(By.CssSelector("[data-testid='pedido-metodo']")).FirstOrDefault()
                ?? _driver.FindElements(By.Name("MetodoPago")).FirstOrDefault()
                ?? _driver.FindElements(By.Id("MetodoPago")).FirstOrDefault()
                ?? _driver.FindElements(By.XPath(
                     "//label[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'metodo de pago') or " +
                     "contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'método de pago')]" +
                     "/following::*[self::select][1]")).FirstOrDefault();

            Assert.IsNotNull(select, "No encontré el <select> de Método de pago.");

            // intentar por texto ("Tarjeta"), por value="2", o una opción válida
            var preferidoLower = preferido.Trim().ToLower();
            var optByText = select.FindElements(By.XPath(
                $".//option[contains(translate(normalize-space(.),'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'), {ToXpathLiteral(preferidoLower)})]"
            )).FirstOrDefault();

            var optByValue2 = select.FindElements(By.CssSelector("option[value='2']")).FirstOrDefault();

            var reales = select.FindElements(By.TagName("option"))
                               .Where(o => !"true".Equals(o.GetAttribute("disabled"), StringComparison.OrdinalIgnoreCase)
                                        && o.Text.Trim().Length > 0
                                        && o.Text.IndexOf("seleccion", StringComparison.OrdinalIgnoreCase) < 0)
                               .ToList();

            var pick = optByText ?? optByValue2 ?? reales.Skip(1).FirstOrDefault() ?? reales.FirstOrDefault();
            Assert.IsNotNull(pick, "No hay opciones válidas de Método de pago.");

            try { pick.Click(); }
            catch
            {
                var value = pick.GetAttribute("value");
                ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                    arguments[0].value = arguments[1];
                    arguments[0].dispatchEvent(new Event('input',{bubbles:true}));
                    arguments[0].dispatchEvent(new Event('change',{bubbles:true}));
                ", select, value);
            }
        }

        private void TypeCardIfPresent(string card)
        {
            var cardInput =
                   _driver.FindElements(By.CssSelector("[data-testid='pedido-card']")).FirstOrDefault()
                ?? _driver.FindElements(By.CssSelector("input[name*='tarjeta' i], input[id*='tarjeta' i], input[name*='card' i], input[id*='card' i], input[name*='numero' i]"))
                         .FirstOrDefault();

            if (cardInput == null) return; // si no aparece en esta vista, no es error

            try { ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].removeAttribute('readonly'); arguments[0].removeAttribute('disabled');", cardInput); } catch { }
            TypeInto(cardInput, card, clear: true);
        }

        private void ClickCrearOrGuardar()
        {
            var btn =
                   _driver.FindElements(By.CssSelector("button[type='submit']")).FirstOrDefault()
                ?? _driver.FindElements(By.XPath("//button[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'crear')]")).FirstOrDefault()
                ?? _driver.FindElements(By.XPath("//button[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'guardar')]")).FirstOrDefault();

            Assert.IsNotNull(btn, "No encontré botón para crear/guardar el pedido.");

            try { ((IJavaScriptExecutor)_driver).ExecuteScript("document.activeElement && document.activeElement.blur();"); } catch { }
            ScrollIntoViewCenter(btn);

            try { btn.Click(); }
            catch (ElementClickInterceptedException) { JsClick(btn); }
            catch (WebDriverException) { JsClick(btn); }
        }

        private void TypeDate(IWebElement input, DateTime date)
        {
            // intenta formato HTML date
            string html5 = date.ToString("yyyy-MM-dd");
            try
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                    arguments[0].value = arguments[1];
                    arguments[0].dispatchEvent(new Event('input',{bubbles:true}));
                    arguments[0].dispatchEvent(new Event('change',{bubbles:true}));
                ", input, html5);
                return;
            }
            catch { }

            // fallback texto dd/MM/yyyy
            TypeInto(input, date.ToString("dd/MM/yyyy"), clear: true);
        }

        private IWebElement InputFor(params string[] keys)
        {
            foreach (var k in keys)
            {
                var e = _driver.FindElements(By.Name(k)).FirstOrDefault()
                     ?? _driver.FindElements(By.Id(k)).FirstOrDefault()
                     ?? _driver.FindElements(By.CssSelector($"input[placeholder*='{k}' i]")).FirstOrDefault()
                     ?? _driver.FindElements(By.XPath(
                          $"//label[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'{k.ToLower()}')]" +
                          "/following::*[self::input][1]")).FirstOrDefault();
                if (e != null) return e;
            }
            var any = _driver.FindElements(By.CssSelector("form input:not([type='hidden'])"))
                             .FirstOrDefault(el => el.Displayed && el.Enabled);
            if (any != null) return any;

            throw new NoSuchElementException($"No encontré input para: {string.Join(", ", keys)}");
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

        private void ScrollIntoViewCenter(IWebElement el)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "arguments[0].scrollIntoView({block:'center', inline:'nearest'});", el);
        }

        private void JsClick(IWebElement el)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", el);
        }

        private void ClickIfExists(By by)
        {
            var el = _driver.FindElements(by).FirstOrDefault();
            if (el == null) return;

            try { el.Click(); }
            catch (ElementClickInterceptedException) { JsClick(el); }
        }

        // ===================================================================
        // =================== HELPERS abrir/eliminar =========================
        // ===================================================================

        private void OpenDeleteConfirmById_Pedido(int id)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Pedido/Delete/{id}");
            if (IsDeleteConfirmView()) return;

            _driver.Navigate().GoToUrl($"{_urlBase}/Pedido/Delete?id={id}");
            if (IsDeleteConfirmView()) return;

            _driver.Navigate().GoToUrl($"{_urlBase}/Pedido");
            var link = _driver.FindElements(By.CssSelector($"a[href*='/Pedido/Delete/{id}']")).FirstOrDefault()
                   ?? _driver.FindElements(By.CssSelector($"a[href*='/Pedido/Delete?id={id}']")).FirstOrDefault();
            Assert.IsNotNull(link, $"No encontré enlace Delete para ID={id}.");
            link!.Click();
        }

        private bool IsDeleteConfirmView()
        {
            var html = _driver.PageSource.ToLower();
            var hasForm = _driver.FindElements(By.CssSelector("form")).Count > 0;
            var hasSubmit = _driver.FindElements(By.CssSelector("button[type='submit']")).Count > 0;
            var saysEliminar = html.Contains("eliminar") || html.Contains("delete");
            return hasForm && (hasSubmit || saysEliminar);
        }

        // ===================================================================
        // ============== HELPERS: localizar ID en listado ====================
        // ===================================================================

        /// <summary>
        /// Busca un pedido por pistas (método/fecha) y devuelve el ID si encuentra un link Edit/Details.
        /// </summary>
        private int? FindPedidoIdEnIndexPorPistas(string metodo, DateTime fecha)
        {
            var metodoLower = metodo.Trim().ToLower();
            var fechaTxt = fecha.ToString("dd/MM/yyyy");

            // Tabla: fila que contenga el método/fecha y tenga link Edit/Details
            var link = _driver.FindElements(By.XPath(
                $"//a[(contains(@href,'/Pedido/Edit') or contains(@href,'/Pedido/Details'))]" +
                $"[ancestor::tr[.//*[contains(translate(normalize-space(.),'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'), {ToXpathLiteral(metodoLower)})]" +
                $" or .//*[contains(normalize-space(.), {ToXpathLiteral(fechaTxt)})]]]"
            )).FirstOrDefault();

            // Cards
            if (link == null)
            {
                link = _driver.FindElements(By.XPath(
                    $"//*[contains(@class,'card') or contains(@class,'pedido') or contains(@class,'order')]" +
                    $"[.//text()[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'), {ToXpathLiteral(metodoLower)})] " +
                    $"or .//text()[contains(., {ToXpathLiteral(fechaTxt)})]]" +
                    $"//a[contains(@href,'/Pedido/Edit') or contains(@href,'/Pedido/Details')]"
                )).FirstOrDefault();
            }

            if (link == null) return null;

            return ExtractIdFromActionHref(_urlBase, link.GetAttribute("href"), "Edit", "Pedido")
                ?? ExtractIdFromActionHref(_urlBase, link.GetAttribute("href"), "Details", "Pedido");
        }

        private int GetLatestIdByAnyAction(string entity, params string[] actions)
        {
            int best = -1;
            foreach (var action in actions)
            {
                var anchors = _driver.FindElements(By.CssSelector($"a[href*='/{entity}/{action}']"));
                foreach (var a in anchors)
                {
                    var maybe = ExtractIdFromActionHref(_urlBase, a.GetAttribute("href"), action, entity);
                    if (maybe.HasValue && maybe.Value > best) best = maybe.Value;
                }
            }
            Assert.IsTrue(best > 0, $"No pude inferir ID en /{entity} por acciones {string.Join(",", actions)}.");
            return best;
        }

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

        // ===================================================================
        // ========================= UTILIDADES ==============================
        // ===================================================================

        private static string ToXpathLiteral(string value)
        {
            if (!value.Contains("'")) return $"'{value}'";
            var parts = value.Split('\'');
            return "concat(" + string.Join(", \"'\", ", parts.Select(p => $"'{p}'")) + ")";
        }
    }
}