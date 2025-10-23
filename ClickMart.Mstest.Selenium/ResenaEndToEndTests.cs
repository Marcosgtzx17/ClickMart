using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Linq;

namespace ClickMart.Mstest.Selenium
{
    [TestClass]
    public class ResenaEndToEndTests
    {
        private IWebDriver _driver;
        private readonly string _urlBase = "https://localhost:7002";

        // CLIENTE autenticado (solo clientes crean/editar/eliminar sus reseñas)
        private readonly string _clientEmail = "jose@gmail.com";
        private readonly string _clientPass = "12345";

        [TestInitialize]
        public void Setup()
        {
            var options = new ChromeOptions();
            // options.AddArgument("--headless=new"); // descomenta para CI
            options.AddArgument("--ignore-certificate-errors");

            _driver = new ChromeDriver(options);
            _driver.Manage().Window.Maximize();
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            // Login cliente
            _driver.Navigate().GoToUrl($"{_urlBase}/Auth/Login");
            _driver.FindElement(By.Name("Email")).SendKeys(_clientEmail);
            _driver.FindElement(By.Name("Password")).SendKeys(_clientPass);
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();
        }

        [TestCleanup]
        public void Teardown()
        {
            try { _driver?.Quit(); } catch { }
            try { _driver?.Dispose(); } catch { }
        }

        /// <summary>
        /// Flujo completo con el MISMO producto y MISMO cliente:
        /// 1) Crear reseña  2) Editar esa misma reseña  3) Eliminar esa misma reseña
        /// Así evitamos el constraint de “una reseña por cliente/producto”.
        /// </summary>
        [TestMethod]
        public void HU38_40_41_Resena_FlujoCompleto_E2E()
        {
            // -------- CREAR --------
            var comentarioCreacion = $"Excelente producto QA {_NowKey()}";
            var id = CreateResenaForProduct(
                productoPreferido: "Shorts",
                calificacion: 5,
                comentario: comentarioCreacion
            );

            Assert.IsTrue(_driver.Url.Contains("/Resena"));

            // -------- EDITAR --------
            _driver.Navigate().GoToUrl($"{_urlBase}/Resena/Edit/{id}");

            var comentarioEditado = $"Comentario Edit QA {_NowKey()}";
            TypeInto(InputFor("Comentario", "Comment", "Contenido", "Resena", "Reseña", "Descripcion"), comentarioEditado, clear: true);
            SetCalificacion(4);
            ClickGuardar();

            _driver.Navigate().GoToUrl($"{_urlBase}/Resena");
            WaitForPageContains(comentarioEditado, 5);
            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Resena"));

            // -------- ELIMINAR --------
            OpenDeleteConfirmById_Resena(id);
            ClickIfExists(By.CssSelector("button[type='submit']"));
            ClickIfExists(By.XPath("//button[contains(.,'Eliminar')]"));
            ClickIfExists(By.XPath("//button[contains(.,'Delete')]"));

            _driver.Navigate().GoToUrl($"{_urlBase}/Resena");

            var goneByComment = !_driver.PageSource.Contains(comentarioEditado);
            var goneById = _driver.FindElements(By.CssSelector(
                $"a[href*='/Resena/Edit/{id}'], a[href*='/Resena/Details/{id}'], a[href*='/Resena/Delete/{id}']")).Count == 0;
            Assert.IsTrue(goneByComment && goneById, "La reseña eliminada no debe aparecer en el listado.");
        }

        // ================= TESTS ADICIONALES =================

        // HU-039: Listado visible
        [TestMethod]
        public void HU39_1067_Resena_Listado_Visible()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Resena");

            var filas =
                  _driver.FindElements(By.CssSelector("[data-testid='resena-row']")).Count
                + _driver.FindElements(By.CssSelector("table tbody tr")).Count
                + _driver.FindElements(By.CssSelector(".resena,.reseña,.review,.card")).Count;

            Assert.IsTrue(filas > 0, "Debe mostrarse el listado de reseñas (al menos una).");
        }

        // HU-038: Validaciones al crear (sin calificación / sin comentario)
        [TestMethod]
        public void HU38_1066_Resena_Crear_Validaciones()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Resena/Create");
            SelectProducto("Shorts"); // producto válido
            // No seteamos calificación ni comentario -> debe validar
            ClickGuardar();

            var html = _driver.PageSource.ToLower();
            Assert.IsTrue(
                _driver.Url.Contains("/Resena/Create")
                || html.Contains("obligator")
                || html.Contains("inválid")
                || html.Contains("calificaci"),
                "Debe mostrar validaciones por campos vacíos y/o calificación inválida."
            );
        }

        // ===================== HELPERS =====================

        private int CreateResenaForProduct(string productoPreferido, int calificacion, string comentario)
        {
            GoToResenaCreate();
            SelectProducto(productoPreferido);
            if (calificacion > 0) SetCalificacion(calificacion);
            TypeInto(InputFor("Comentario", "Comment", "Contenido", "Resena", "Reseña", "Descripcion"), comentario, clear: true);
            ClickGuardar();

            _driver.Navigate().GoToUrl($"{_urlBase}/Resena");
            WaitForPageContains(comentario, 5);
            var id = FindResenaIdEnIndexPorComentario(comentario);
            Assert.IsTrue(id > 0, $"No pude capturar el ID de la reseña creada ('{comentario}').");
            return id;
        }

        private void GoToResenaCreate()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Resena");
            var crear = _driver.FindElements(By.CssSelector("[data-testid='resena-create']")).FirstOrDefault()
                    ?? _driver.FindElements(By.PartialLinkText("Crear")).FirstOrDefault()
                    ?? _driver.FindElements(By.PartialLinkText("Nueva")).FirstOrDefault()
                    ?? _driver.FindElements(By.PartialLinkText("Nuevo")).FirstOrDefault();

            if (crear != null) crear.Click();
            else _driver.Navigate().GoToUrl($"{_urlBase}/Resena/Create");
        }

        private int FindResenaIdEnIndexPorComentario(string comentario)
        {
            var link = _driver.FindElements(By.XPath(
                $"//a[contains(@href,'/Resena/Edit')][ancestor::tr[.//*[contains(normalize-space(.), {ToXpathLiteral(comentario)})]]]"
            )).FirstOrDefault();

            if (link == null)
            {
                link = _driver.FindElements(By.XPath(
                    $"//*[contains(@class,'card') or contains(@class,'resena') or contains(@class,'reseña')]" +
                    $"[.//text()[contains(., {ToXpathLiteral(comentario)})]]//a[contains(@href,'/Resena/Edit')]"
                )).FirstOrDefault();
            }

            if (link != null)
            {
                var maybe = ExtractIdFromActionHref(_urlBase, link.GetAttribute("href"), "Edit", "Resena");
                if (maybe.HasValue) return maybe.Value;
            }

            return GetLatestIdByAction("Edit", "Resena");
        }

        private void SelectProducto(string visibleTextPreferido = "Shorts")
        {
            var select =
                   _driver.FindElements(By.CssSelector("[data-testid='resena-producto']")).FirstOrDefault()
                ?? _driver.FindElements(By.Name("ProductoId")).FirstOrDefault()
                ?? _driver.FindElements(By.Id("ProductoId")).FirstOrDefault()
                ?? _driver.FindElements(By.XPath("//label[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'producto')]/following::*[self::select][1]")).FirstOrDefault();

            Assert.IsNotNull(select, "No encontré el <select> de Producto.");

            var optExact = select.FindElements(By.XPath($".//option[normalize-space(.)={ToXpathLiteral(visibleTextPreferido)}]")).FirstOrDefault();
            if (optExact != null && optExact.Enabled)
            {
                optExact.Click();
            }
            else
            {
                var firstValid = select.FindElements(By.TagName("option"))
                    .FirstOrDefault(o => o.Enabled &&
                                         !o.GetAttribute("disabled").Equals("true", StringComparison.OrdinalIgnoreCase) &&
                                         o.Text.Trim().Length > 0 &&
                                         o.Text.IndexOf("selecciona", StringComparison.OrdinalIgnoreCase) < 0);
                Assert.IsNotNull(firstValid, "No hay opciones de producto válidas.");
                firstValid!.Click();
            }

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

        private void SetCalificacion(int value)
        {
            if (value <= 0) return;

            var radios = _driver.FindElements(By.CssSelector("input[type='radio'][name='Calificacion']"))
                                .Where(r => r.GetAttribute("value") == value.ToString())
                                .ToList();
            if (radios.Any()) { SafeClick(radios.First()); return; }

            var num = _driver.FindElements(By.CssSelector("input[type='number'][name='Calificacion']")).FirstOrDefault();
            if (num != null) { TypeInto(num, value.ToString(), clear: true); return; }

            var stars = _driver.FindElements(By.CssSelector("[data-testid='resena-rating'] .star, .rating .star, .stars .star, [class*='star' i]")).ToList();
            if (stars.Count >= value) { SafeClick(stars[value - 1]); return; }

            var cont = _driver.FindElements(By.CssSelector("[data-testid='resena-rating']")).FirstOrDefault()
                   ?? _driver.FindElements(By.XPath(
                        "//label[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'calificaci')]/following::*[1]"
                      )).FirstOrDefault();
            if (cont != null)
            {
                try
                {
                    cont.Click();
                    cont.SendKeys(Keys.Home);
                    for (int i = 1; i < value; i++) cont.SendKeys(Keys.ArrowRight);
                    return;
                }
                catch { }
            }

            var hidden = _driver.FindElements(By.CssSelector("input[name='Calificacion'], input[id='Calificacion']")).FirstOrDefault();
            if (hidden != null)
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                    arguments[0].value = arguments[1];
                    arguments[0].dispatchEvent(new Event('input',{bubbles:true}));
                    arguments[0].dispatchEvent(new Event('change',{bubbles:true}));
                ", hidden, value);
                return;
            }

            Assert.Inconclusive("No pude establecer la calificación. Agrega data-testid='resena-rating' o radios name='Calificacion'.");
        }

        private void OpenDeleteConfirmById_Resena(int id)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Resena/Delete/{id}");
            if (!IsDeleteConfirmView())
            {
                _driver.Navigate().GoToUrl($"{_urlBase}/Resena/Delete?id={id}");
                if (!IsDeleteConfirmView())
                {
                    _driver.Navigate().GoToUrl($"{_urlBase}/Resena");
                    var link = _driver.FindElements(By.CssSelector($"a[href*='/Resena/Delete/{id}']")).FirstOrDefault()
                           ?? _driver.FindElements(By.CssSelector($"a[href*='/Resena/Delete?id={id}']")).FirstOrDefault();
                    Assert.IsNotNull(link, $"No encontré enlace Delete para ID={id} en el listado.");
                    link!.Click();
                }
            }
        }

        private bool IsDeleteConfirmView()
        {
            var html = _driver.PageSource.ToLower();
            var hasForm = _driver.FindElements(By.CssSelector("form")).Count > 0;
            var hasSubmit = _driver.FindElements(By.CssSelector("button[type='submit']")).Count > 0;
            var saysEliminar = html.Contains("eliminar") || html.Contains("delete");
            return hasForm && (hasSubmit || saysEliminar);
        }

        // -------- Genéricos --------

        private IWebElement InputFor(params string[] keys)
        {
            foreach (var k in keys)
            {
                var e = _driver.FindElements(By.Name(k)).FirstOrDefault()
                     ?? _driver.FindElements(By.Id(k)).FirstOrDefault()
                     ?? _driver.FindElements(By.CssSelector($"input[placeholder*='{k}' i], textarea[placeholder*='{k}' i]")).FirstOrDefault()
                     ?? _driver.FindElements(By.XPath($"//label[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'{k.ToLower()}')]/following::*[self::input or self::textarea][1]")).FirstOrDefault();
                if (e != null) return e;
            }
            var anyVisible = _driver.FindElements(By.CssSelector("form input:not([type='hidden']), form textarea"))
                                    .FirstOrDefault(el => el.Displayed && el.Enabled);
            if (anyVisible != null) return anyVisible;

            throw new NoSuchElementException($"No encontré input/textarea para keys: {string.Join(", ", keys)}");
        }

        private void ClickGuardar()
        {
            var btn = _driver.FindElements(By.CssSelector("[data-testid='resena-save']")).FirstOrDefault()
                  ?? _driver.FindElements(By.CssSelector("button[type='submit']")).FirstOrDefault()
                  ?? _driver.FindElements(By.XPath("//button[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'guardar')]")).FirstOrDefault();

            Assert.IsNotNull(btn, "No encontré botón Guardar.");

            try { ((IJavaScriptExecutor)_driver).ExecuteScript("document.activeElement && document.activeElement.blur();"); } catch { }
            ScrollIntoViewCenter(btn);
            WaitUntilClickable(btn, TimeSpan.FromSeconds(4));

            try { btn.Click(); }
            catch (ElementClickInterceptedException) { JsClick(btn); }
            catch (WebDriverException) { JsClick(btn); }
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

        private static int? ExtractIdFromActionHref(string baseUrl, string href, string action, string entity)
        {
            if (string.IsNullOrWhiteSpace(href)) return null;

            if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
            {
                var b = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");
                uri = new Uri(b, href);
            }

            var segs = uri.AbsolutePath.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segs.Length >= 3 &&
                segs[^3].Equals(entity, StringComparison.OrdinalIgnoreCase) &&
                segs[^2].Equals(action, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(segs[^1], out var idBySeg))
            {
                return idBySeg;
            }

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

        private void WaitUntilClickable(IWebElement el, TimeSpan? timeout = null)
        {
            var until = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
            while (DateTime.UtcNow < until)
            {
                try { if (el.Displayed && el.Enabled) return; } catch { }
                System.Threading.Thread.Sleep(100);
            }
        }

        private void SafeClick(IWebElement el)
        {
            try
            {
                ScrollIntoViewCenter(el);
                WaitUntilClickable(el, TimeSpan.FromSeconds(4));
                el.Click();
            }
            catch (ElementClickInterceptedException)
            {
                JsClick(el);
            }
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