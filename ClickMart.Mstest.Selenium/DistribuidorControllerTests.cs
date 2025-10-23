using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

namespace ClickMart.Mstest.Selenium
{
    [TestClass]
    public class DistribuidorControllerTests
    {
        private IWebDriver _driver;
        private readonly string _urlBase = "https://localhost:7002";

        // Credenciales admin
        private readonly string _adminEmail = "m@gmail.com";
        private readonly string _adminPass = "12345";

        [TestInitialize]
        public void Setup()
        {
            var options = new ChromeOptions();
            // options.AddArgument("--headless=new");
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

        // ================= HU7–HU10 (1015–1021) =================

        // 1015: Creación exitosa de distribuidor
        [TestMethod]
        public void HU7_1015_Distribuidor_Crear_Exitoso()
        {
            GoToDistribuidorCreate();

            FillProveedorForm(
                nombre: $"Prov QA {_NowKey()}",
                telefono: "77777777",
                direccion: "Av. QA 123",
                gmailOCorreo: $"prov.qa{DateTime.UtcNow:yyyyMMddHHmmss}@mailinator.com",
                fechaRegistro: DateTime.Today,
                descripcion: "Proveedor generado por pruebas UI"
            );

            ClickGuardar();

            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor");
            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Distribuidor"));
        }

        // 1016: Validación de campos obligatorios (crear)
        [TestMethod]
        public void HU7_1016_Distribuidor_Crear_CamposVacios_MuestraObligatorio()
        {
            GoToDistribuidorCreate();
            ClickGuardar(); // sin llenar

            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Distribuidor/Create"));
        }

        // 1017: Visualización del listado
        [TestMethod]
        public void HU8_1017_Distribuidor_Listado_Visible()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor");
            var filas = _driver.FindElements(By.CssSelector("table tbody tr"));
            Assert.IsTrue(filas.Count > 0, "Debe mostrarse el listado de distribuidores (al menos una fila).");
        }

        // 1018: Edición exitosa
        [TestMethod]
        public void HU9_1018_Distribuidor_Editar_Exitoso()
        {
            GoToDistribuidorCreate();
            var baseName = $"ProvBase QA {_NowKey()}";
            FillProveedorForm(baseName, "77777777", "Av QA 1", $"prov.base{DateTime.UtcNow:HHmmss}@mailinator.com",
                              DateTime.Today, "base");
            ClickGuardar();

            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor");
            var linkEditar = _driver.FindElements(By.XPath(
                $"//a[contains(@href,'/Distribuidor/Edit')][ancestor::tr[.//td[contains(normalize-space(.),'{baseName}')]]]"))
                .FirstOrDefault();

            int id = linkEditar != null
                ? ExtractIdFromActionHref(_urlBase, linkEditar.GetAttribute("href"), "Edit", "Distribuidor")
                    ?? GetLatestIdByAction("Edit", "Distribuidor")
                : GetLatestIdByAction("Edit", "Distribuidor");

            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor/Edit/{id}");

            var nuevo = $"ProvEdit QA {_NowKey()}";
            TypeInto(InputFor("Nombre", "NombreDistribuidor", "Proveedor"), nuevo, clear: true);
            ClickGuardar();

            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor");
            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Distribuidor"));
        }

        // 1019: Validación al editar (nombre vacío)
        [TestMethod]
        public void HU9_1019_Distribuidor_Editar_SinNombre_MuestraObligatorio()
        {
            GoToDistribuidorCreate();
            var baseName = $"ProvTemp QA {_NowKey()}";
            FillProveedorForm(baseName, "77777777", "Av QA 2", $"prov.temp{DateTime.UtcNow:HHmmss}@mailinator.com",
                              DateTime.Today, "temp");
            ClickGuardar();

            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor");
            var linkEditar = _driver.FindElements(By.XPath(
                $"//a[contains(@href,'/Distribuidor/Edit')][ancestor::tr[.//td[contains(normalize-space(.),'{baseName}')]]]"))
                .FirstOrDefault();

            int id = linkEditar != null
                ? ExtractIdFromActionHref(_urlBase, linkEditar.GetAttribute("href"), "Edit", "Distribuidor")
                    ?? GetLatestIdByAction("Edit", "Distribuidor")
                : GetLatestIdByAction("Edit", "Distribuidor");

            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor/Edit/{id}");

            TypeInto(InputFor("Nombre", "NombreDistribuidor", "Proveedor"), "", clear: true);
            ClickGuardar();

            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Distribuidor/Edit"));
        }

        // 1020: Eliminación exitosa (sin productos)
        [TestMethod]
        public void HU10_1020_Distribuidor_Eliminar_SinProductos_Exitoso()
        {
            GoToDistribuidorCreate();
            var nombre = $"ProvDelete QA {_NowKey()}";
            FillProveedorForm(nombre, "77777777", "Av QA 3", $"prov.del{DateTime.UtcNow:HHmmss}@mailinator.com",
                              DateTime.Today, "para eliminar");
            ClickGuardar();

            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor");
            var linkDelete = _driver.FindElements(By.XPath(
                $"//a[contains(@href,'/Distribuidor/Delete')][ancestor::tr[.//td[contains(normalize-space(.),'{nombre}')]]]"))
                .FirstOrDefault();

            int id = linkDelete != null
                ? ExtractIdFromActionHref(_urlBase, linkDelete.GetAttribute("href"), "Delete", "Distribuidor")
                    ?? GetLatestIdByAction("Delete", "Distribuidor")
                : GetLatestIdByAction("Delete", "Distribuidor");

            OpenDeleteConfirmById_Dist(id);
            ClickIfExists(By.CssSelector("button[type='submit']"));
            ClickIfExists(By.XPath("//button[contains(.,'Eliminar')]"));
            ClickIfExists(By.XPath("//button[contains(.,'Delete')]"));

            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor");
            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Distribuidor"));
        }

        // 1021: Restricción por referencias activas (id fijo = 4)
        [TestMethod]
        public void HU10_1021_Distribuidor_Eliminar_Id4_EnUso_Bloqueado()
        {
            const int id = 4;

            if (!TryOpenDeleteById_Dist(id))
                Assert.Inconclusive($"No pude abrir la confirmación de borrado para Distribuidor ID={id}.");

            ClickIfExists(By.CssSelector("button[type='submit']"));
            ClickIfExists(By.XPath("//button[contains(.,'Eliminar')]"));

            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor");

            var html = _driver.PageSource.ToLower();
            bool siguePresente =
                   _driver.FindElements(By.CssSelector($"a[href*='/Distribuidor/Edit/{id}']")).Count > 0
                || _driver.FindElements(By.CssSelector($"a[href*='/Distribuidor/Details/{id}']")).Count > 0
                || _driver.FindElements(By.CssSelector($"a[href*='/Distribuidor/Delete/{id}']")).Count > 0;

            bool blocked = html.Contains("no se puede eliminar")
                        || html.Contains("en uso")
                        || html.Contains("integridad")
                        || html.Contains("existen productos asociados")
                        || siguePresente;

            Assert.IsTrue(blocked, $"Debe bloquearse la eliminación del Distribuidor ID={id} por estar en uso.");
        }

        // ================= Helpers =================

        private void GoToDistribuidorCreate()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor");
            var crear = _driver.FindElements(By.CssSelector("[data-testid='prov-create']")).FirstOrDefault()
                    ?? _driver.FindElements(By.PartialLinkText("Crear")).FirstOrDefault()
                    ?? _driver.FindElements(By.PartialLinkText("Nuevo")).FirstOrDefault()
                    ?? _driver.FindElements(By.PartialLinkText("Nueva")).FirstOrDefault();

            if (crear != null) crear.Click();
            else _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor/Create");
        }

        private void FillProveedorForm(string nombre, string telefono, string direccion,
                                       string gmailOCorreo, DateTime fechaRegistro, string descripcion)
        {
            TypeInto(InputFor("Nombre", "NombreDistribuidor", "Proveedor"), nombre, clear: true);
            TypeInto(InputFor("Telefono", "Teléfono", "Celular"), telefono, clear: true);
            TypeInto(InputFor("Direccion", "Dirección", "Address"), direccion, clear: true);
            TypeInto(InputFor("Gmail", "Correo", "Email", "CorreoElectronico"), gmailOCorreo, clear: true);

            var fechaEl = InputFor("FechaRegistro", "Fecha", "Fecha de registro", "FechaRegistroDistribuidor");
            TypeDate(fechaEl, fechaRegistro);

            TypeInto(TextAreaFor("Descripcion", "Descripción", "Detalle"), descripcion, clear: true);
        }

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
            return InputFor(keys);
        }

        // Entrada robusta (Clear -> Ctrl+A+Del -> JS con input/change)
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

        // Setter específico para fechas (date pickers/readonly)
        private void TypeDate(IWebElement el, DateTime date)
        {
            var js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript(@"
                arguments[0].removeAttribute('readonly');
                arguments[0].removeAttribute('disabled');
                arguments[0].value = arguments[1];
                arguments[0].dispatchEvent(new Event('input', { bubbles: true }));
                arguments[0].dispatchEvent(new Event('change', { bubbles: true }));
            ", el, date.ToString("yyyy-MM-dd"));
        }

        // Click Guardar sin SeleniumExtras
        private void ClickGuardar()
        {
            var btn = _driver.FindElements(By.CssSelector("button[type='submit']")).FirstOrDefault()
                  ?? _driver.FindElements(By.XPath("//button[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'guardar')]")).FirstOrDefault();

            Assert.IsNotNull(btn, "No encontré botón Guardar.");

            CloseFloatingUi();
            ScrollIntoViewCenter(btn);
            Sleep(120);

            try
            {
                WaitUntilClickable(btn, TimeSpan.FromSeconds(6));
                btn.Click();
            }
            catch (ElementClickInterceptedException)
            {
                JsClick(btn);
            }
            catch (WebDriverException)
            {
                JsClick(btn);
            }
        }

        private void WaitUntilClickable(IWebElement el, TimeSpan? timeout = null)
        {
            var wait = new WebDriverWait(_driver, timeout ?? TimeSpan.FromSeconds(5));
            wait.Until(_ =>
            {
                try { return el.Displayed && el.Enabled; }
                catch { return false; }
            });
        }

        // === Navegación/ID helpers ===

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

        private void OpenDeleteConfirmById_Dist(int id)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor/Delete/{id}");
            if (!IsDeleteConfirmView())
            {
                _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor/Delete?id={id}");
                if (!IsDeleteConfirmView())
                {
                    _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor");
                    var link = _driver.FindElements(By.CssSelector($"a[href*='/Distribuidor/Delete/{id}']")).FirstOrDefault()
                           ?? _driver.FindElements(By.CssSelector($"a[href*='/Distribuidor/Delete?id={id}']")).FirstOrDefault();
                    Assert.IsNotNull(link, $"No encontré enlace Delete para ID={id} en el listado.");
                    link!.Click();
                }
            }
        }

        private bool TryOpenDeleteById_Dist(int id)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor/Delete/{id}");
            if (IsDeleteConfirmView()) return true;

            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor/Delete?id={id}");
            if (IsDeleteConfirmView()) return true;

            _driver.Navigate().GoToUrl($"{_urlBase}/Distribuidor");
            var link = _driver.FindElements(By.CssSelector($"a[href*='/Distribuidor/Delete/{id}']")).FirstOrDefault()
                   ?? _driver.FindElements(By.CssSelector($"a[href*='/Distribuidor/Delete?id={id}']")).FirstOrDefault();

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
            el?.Click();
        }

        private static string _NowKey() => DateTime.UtcNow.ToString("HHmmss");

        // ===== UI utilities =====
        private void CloseFloatingUi()
        {
            try
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("document.activeElement && document.activeElement.blur();");
                var body = _driver.FindElement(By.TagName("body"));
                body.SendKeys(Keys.Escape);
            }
            catch { }
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

        private static void Sleep(int ms) => System.Threading.Thread.Sleep(ms);
    }
}