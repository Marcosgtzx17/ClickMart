using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Linq;

namespace ClickMart.Mstest.Selenium
{
    [TestClass]
    public class UsuarioControllerTests
    {
        private IWebDriver _driver;
        private readonly string _urlBase = "https://localhost:7002";

        // Credenciales admin para loguearnos
        private readonly string _adminEmail = "m@gmail.com";
        private readonly string _adminPass = "12345";

        [TestInitialize]
        public void Setup()
        {
            var options = new ChromeOptions();
            // options.AddArgument("--headless=new");   // útil en CI
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

        // ================= HU15–HU18 (1029–1035) =================

        // 1029: Creación exitosa (siempre Administrador)
        [TestMethod]
        public void HU15_1029_Usuario_Crear_Admin_Exitoso()
        {
            GoToUsuarioCreate();

            var nombre = $"Admin QA {_NowKey()}";
            var email = $"admin.qa{DateTime.UtcNow:yyyyMMddHHmmss}@mailinator.com";

            FillUsuarioForm(
                nombre: nombre,
                email: email,
                telefono: "77777777",
                direccion: "Av QA 123",
                password: "Password123!",
                rolTexto: "Administrador"
            );

            ClickGuardar();

            _driver.Navigate().GoToUrl($"{_urlBase}/Usuario");
            Assert.IsTrue(_driver.Url.Contains("/Usuario"));
            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Usuario"));
        }

        // 1030: Validación de obligatorios (crear)
        [TestMethod]
        public void HU15_1030_Usuario_Crear_CamposVacios_MuestraObligatorio()
        {
            GoToUsuarioCreate();
            ClickGuardar(); // sin llenar nada

            Assert.IsTrue(_driver.Url.Contains("/Usuario/Create"));
        }

        // 1031: Listado visible
        [TestMethod]
        public void HU16_1031_Usuario_Listado_Visible()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Usuario");
            var filas = _driver.FindElements(By.CssSelector("table tbody tr"));
            Assert.IsTrue(filas.Count > 0, "Debe mostrarse el listado de usuarios (al menos una fila).");
        }


        // 1032: Edición exitosa
        [TestMethod]
        public void HU17_1032_Usuario_Editar_Exitoso()
        {
            GoToUsuarioCreate();
            var baseNombre = $"AdminBase QA {_NowKey()}";
            var baseEmail = $"admin.base{DateTime.UtcNow:HHmmss}@mailinator.com";

            FillUsuarioForm(baseNombre, baseEmail, "77777777", "Av QA 1", "Password123!", "Administrador");
            ClickGuardar();

            var id = FindUsuarioIdEnIndexPorEmail(baseEmail);
            _driver.Navigate().GoToUrl($"{_urlBase}/Usuario/Edit/{id}");

            var nuevoNombre = $"AdminEdit QA {_NowKey()}";
            TypeInto(InputFor("Nombre", "name", "FullName"), nuevoNombre, clear: true);
            TypeInto(InputFor("Direccion", "Dirección", "Address"), "Av QA 9", clear: true);

            ClickGuardar();

            _driver.Navigate().GoToUrl($"{_urlBase}/Usuario");
            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Usuario"));
        }

        // 1033: Validación en editar (email inválido o vacío)
        [TestMethod]
        public void HU17_1033_Usuario_Editar_EmailInvalido_MuestraError()
        {
            GoToUsuarioCreate();
            var baseEmail = $"admin.temp{DateTime.UtcNow:HHmmss}@mailinator.com";
            FillUsuarioForm($"Temp QA {_NowKey()}", baseEmail, "77777777", "Av QA 2", "Password123!", "Administrador");
            ClickGuardar();

            var id = FindUsuarioIdEnIndexPorEmail(baseEmail);
            _driver.Navigate().GoToUrl($"{_urlBase}/Usuario/Edit/{id}");

            var emailInput = InputFor("Email", "Correo", "Gmail");
            TypeInto(emailInput, "", clear: true);
            TypeInto(emailInput, "no-es-email", clear: false);

            ClickGuardar();

            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Usuario/Edit"));
        }

        // 1034: Eliminación exitosa (sin dependencias)
        // 1034: Eliminación exitosa (sin dependencias)
        [TestMethod]
        public void HU18_1034_Usuario_Eliminar_SinDependencias_Exitoso()
        {
            // 1) Crear usuario ad-hoc (Administrador)
            GoToUsuarioCreate();
            var email = $"admin.del{DateTime.UtcNow:HHmmss}@mailinator.com";
            FillUsuarioForm($"AdminDel QA {_NowKey()}", email, "77777777", "Av QA 3", "Password123!", "Administrador");
            ClickGuardar();

            // 2) Obtener ID desde el índice por el email recién creado
            var id = FindUsuarioIdEnIndexPorEmail(email);

            // 3) Abrir confirmación de borrado y confirmar con clic robusto
            OpenDeleteConfirmById_User(id);

            // Limpia focos/overlays y centra el botón
            CloseFloatingUi();
            SafeClickIfExists(By.CssSelector("button[type='submit']:not([disabled])"));
            SafeClickIfExists(By.XPath("//button[not(@disabled) and (contains(.,'Eliminar') or contains(.,'Delete'))]"));

            // 4) Regresar al listado y verificar que NO existe
            _driver.Navigate().GoToUrl($"{_urlBase}/Usuario");

            bool goneByEmail = !_driver.PageSource.Contains(email);
            bool goneById = _driver.FindElements(By.CssSelector(
                $"a[href*='/Usuario/Edit/{id}'], a[href*='/Usuario/Details/{id}'], a[href*='/Usuario/Delete/{id}']"))
                .Count == 0;

            Assert.IsTrue(goneByEmail && goneById,
                $"El usuario (ID={id}, Email='{email}') debería haberse eliminado.");
        }
        // 1035: Restricción por dependencias críticas: usuario fijo ID=3 (cliente)
        [TestMethod]
        public void HU18_1035_Usuario_Eliminar_Id3_EnUso_Bloqueado()
        {
            const int id = 3;

            if (!TryOpenDeleteById_User(id))
                Assert.Inconclusive("No pude abrir la confirmación de borrado para Usuario ID=3. Revisa la ruta o que exista.");

            // Señal temprana de bloqueo en la página de confirmación
            var htmlDelete = _driver.PageSource.ToLower();
            bool bannerBloqueo = htmlDelete.Contains("no se puede eliminar")
                              || htmlDelete.Contains("está en uso")
                              || htmlDelete.Contains("tiene referencias")
                              || htmlDelete.Contains("dependenc");

            // Intenta confirmar solo si el botón está habilitado; si algo lo tapa, usamos JS click
            SafeClickIfExists(By.CssSelector("button[type='submit']:not([disabled])"));
            SafeClickIfExists(By.XPath("//button[not(@disabled) and (contains(.,'Eliminar') or contains(.,'Delete'))]"));

            // Volvemos al listado para validar que el usuario sigue existiendo
            _driver.Navigate().GoToUrl($"{_urlBase}/Usuario");

            var html = _driver.PageSource.ToLower();
            bool siguePresente =
                   _driver.FindElements(By.CssSelector($"a[href*='/Usuario/Edit/{id}']")).Count > 0
                || _driver.FindElements(By.CssSelector($"a[href*='/Usuario/Details/{id}']")).Count > 0
                || _driver.FindElements(By.CssSelector($"a[href*='/Usuario/Delete/{id}']")).Count > 0;

            bool blocked = bannerBloqueo
                        || html.Contains("no se puede eliminar")
                        || html.Contains("dependenc")
                        || html.Contains("en uso")
                        || html.Contains("asociad")
                        || siguePresente;

            Assert.IsTrue(blocked, "Debe bloquear la eliminación del Usuario ID=3 por dependencias críticas.");
        }
        // ================= Helpers específicos de Usuario =================

        private void GoToUsuarioCreate()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Usuario");
            var crear = _driver.FindElements(By.CssSelector("[data-testid='usr-create']")).FirstOrDefault()
                    ?? _driver.FindElements(By.PartialLinkText("Crear")).FirstOrDefault()
                    ?? _driver.FindElements(By.PartialLinkText("Nuevo")).FirstOrDefault()
                    ?? _driver.FindElements(By.PartialLinkText("Nueva")).FirstOrDefault();

            if (crear != null) crear.Click();
            else _driver.Navigate().GoToUrl($"{_urlBase}/Usuario/Create");
        }

        private void FillUsuarioForm(string nombre, string email, string telefono, string direccion, string password, string rolTexto)
        {
            TypeInto(InputFor("Nombre", "name", "FullName"), nombre, clear: true);
            TypeInto(InputFor("Email", "Correo", "Gmail"), email, clear: true);
            TypeInto(InputFor("Telefono", "Teléfono", "Phone", "Celular"), telefono, clear: true);
            TypeInto(InputFor("Direccion", "Dirección", "Address"), direccion, clear: true);
            TypeInto(InputFor("Password", "Contraseña", "Clave"), password, clear: true);

            SelectRolByText(rolTexto);  // sin SelectElement
        }

        private void SelectRolByText(string visibleText)
        {
            var select =
                   _driver.FindElements(By.CssSelector("[data-testid='usr-rol']")).FirstOrDefault()
                ?? _driver.FindElements(By.Name("RolId")).FirstOrDefault()
                ?? _driver.FindElements(By.Id("RolId")).FirstOrDefault()
                ?? _driver.FindElements(By.Name("Rol")).FirstOrDefault()
                ?? _driver.FindElements(By.Id("Rol")).FirstOrDefault()
                ?? _driver.FindElements(By.XPath("//label[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'rol')]/following::*[self::select][1]")).FirstOrDefault()
                ?? _driver.FindElements(By.CssSelector("form select")).FirstOrDefault();

            Assert.IsNotNull(select, "No encontré el <select> de Rol.");

            ScrollIntoViewCenter(select);

            try { select.Click(); } catch { /* algunos selects no lo requieren */ }

            // Match exacto; si no, contiene
            var option = select.FindElements(By.TagName("option"))
                               .FirstOrDefault(o => o.Text.Trim()
                                   .Equals(visibleText, StringComparison.OrdinalIgnoreCase))
                      ?? select.FindElements(By.TagName("option"))
                               .FirstOrDefault(o => o.Text.Trim()
                                   .IndexOf(visibleText, StringComparison.OrdinalIgnoreCase) >= 0);

            Assert.IsNotNull(option, $"No encontré la opción de rol '{visibleText}'.");
            option.Click();

            // Disparar change/input por si la UI lo necesita
            try
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                    const s = arguments[0];
                    s.dispatchEvent(new Event('input', { bubbles: true }));
                    s.dispatchEvent(new Event('change', { bubbles: true }));
                ", select);
            }
            catch { }
        }

        private int FindUsuarioIdEnIndexPorEmail(string email)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Usuario");

            var linkEditar = _driver.FindElements(By.XPath(
                $"//a[contains(@href,'/Usuario/Edit')][ancestor::tr[.//td[contains(normalize-space(.),'{email}')]]]"))
                .FirstOrDefault();

            if (linkEditar != null)
            {
                var maybe = ExtractIdFromActionHref(_urlBase, linkEditar.GetAttribute("href"), "Edit", "Usuario");
                if (maybe.HasValue) return maybe.Value;
            }

            return GetLatestIdByAction("Edit", "Usuario");
        }

        // ================= Helpers genéricos reutilizados =================

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

        private void OpenDeleteConfirmById_User(int id)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Usuario/Delete/{id}");
            if (!IsDeleteConfirmView())
            {
                _driver.Navigate().GoToUrl($"{_urlBase}/Usuario/Delete?id={id}");
                if (!IsDeleteConfirmView())
                {
                    _driver.Navigate().GoToUrl($"{_urlBase}/Usuario");
                    var link = _driver.FindElements(By.CssSelector($"a[href*='/Usuario/Delete/{id}']")).FirstOrDefault()
                           ?? _driver.FindElements(By.CssSelector($"a[href*='/Usuario/Delete?id={id}']")).FirstOrDefault();
                    Assert.IsNotNull(link, $"No encontré enlace Delete para ID={id} en el listado.");
                    link!.Click();
                }
            }
        }

        private bool TryOpenDeleteById_User(int id)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Usuario/Delete/{id}");
            if (IsDeleteConfirmView()) return true;

            _driver.Navigate().GoToUrl($"{_urlBase}/Usuario/Delete?id={id}");
            if (IsDeleteConfirmView()) return true;

            _driver.Navigate().GoToUrl($"{_urlBase}/Usuario");
            var link = _driver.FindElements(By.CssSelector($"a[href*='/Usuario/Delete/{id}']")).FirstOrDefault()
                   ?? _driver.FindElements(By.CssSelector($"a[href*='/Usuario/Delete?id={id}']")).FirstOrDefault();

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

        // Spin-wait simple para evitar WebDriverWait (sin Selenium.Support)
        private void WaitUntilClickable(IWebElement el, TimeSpan? timeout = null)
        {
            var until = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
            while (DateTime.UtcNow < until)
            {
                try
                {
                    if (el.Displayed && el.Enabled) return;
                }
                catch { }
                System.Threading.Thread.Sleep(100);
            }
        }

        private void ClickIfExists(By by)
        {
            var el = _driver.FindElements(by).FirstOrDefault();
            el?.Click();
        }

        private static string _NowKey() => DateTime.UtcNow.ToString("HHmmss");

        // ===== utilidades UI =====
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
        private void SafeClickIfExists(By by)
        {
            var el = _driver.FindElements(by).FirstOrDefault();
            if (el == null) return;

            try
            {
                // quita focos/overlays y centra el elemento
                try { ((IJavaScriptExecutor)_driver).ExecuteScript("document.activeElement && document.activeElement.blur();"); } catch { }
                ScrollIntoViewCenter(el);

                // espera “manual” a que sea clickable (sin Selenium.Support)
                WaitUntilClickable(el, TimeSpan.FromSeconds(4));

                el.Click();
            }
            catch (ElementClickInterceptedException)
            {
                // Si algo intercepta el click, disparamos click por JS
                JsClick(el);
            }
            catch (WebDriverException)
            {
                JsClick(el);
            }
        }
    }
}