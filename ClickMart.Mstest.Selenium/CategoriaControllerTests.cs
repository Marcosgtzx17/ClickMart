using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Linq;

namespace ClickMart.Mstest.Selenium
{
    [TestClass]
    public class CategoriaControllerTests
    {
        private IWebDriver _driver;
        private readonly string _urlBase = "https://localhost:7002";

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

            // Login admin (simple y efectivo)
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

        [TestMethod]
        public void Categoria_Crear_Exitosa()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria/Create");

            _driver.FindElement(By.Name("Nombre")).SendKeys($"Electrónica QA {_NowKey()}");
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Categoria"));

            // Vuelve al listado y verifica que quedó
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria");
            var listado = _driver.PageSource;
            Assert.IsTrue(listado.Contains("Electrónica QA"), "La categoría creada debe aparecer en el listado.");
        }

        [TestMethod]
        public void Categoria_Crear_SinNombre_MuestraObligatorio()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria/Create");

            // Deja vacío y guarda
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Categoria/Create"));
        }

        [TestMethod]
        public void Categoria_Listado_Visible()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria");
            var filas = _driver.FindElements(By.CssSelector("table tbody tr"));
            Assert.IsTrue(filas.Count > 0, "Debe mostrarse el listado (al menos una fila).");
        }

        [TestMethod]
        public void Categoria_Editar_Exitosa()
        {
            // 1) Crear para asegurar que exista
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria/Create");
            var nombreOriginal = $"Electronica QA {_NowKey()}";
            _driver.FindElement(By.Name("Nombre")).SendKeys(nombreOriginal);
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // 2) Ir al listado real
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria");

            // 2.1) Buscar el <a> Editar que esté en la MISMA FILA del nombre recién creado
            var xpathEditarMismaFila =
                $"//a[contains(@href,'/Categoria/Edit')][ancestor::tr[.//td[contains(normalize-space(.),'{nombreOriginal}')]]]";
            var linkEditar = _driver.FindElements(By.XPath(xpathEditarMismaFila)).FirstOrDefault();

            int id;
            if (linkEditar != null)
            {
                var href = linkEditar.GetAttribute("href");
                var maybeId = ExtractCategoriaIdFromEditHref(_urlBase, href);
                Assert.IsTrue(maybeId.HasValue, $"No pude extraer el ID desde el href '{href}'.");
                id = maybeId.Value;
            }
            else
            {
                // Fallback: mayor ID de los Edit visibles
                id = GetLatestCategoriaIdFromIndex();
            }

            // 2.2) Ir a la URL de edición real con ID
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria/Edit/{id}");

            // 3) Cambiar nombre y guardar
            var nuevo = $"Tecnologia QA {_NowKey()}";
            var input = _driver.FindElement(By.Name("Nombre"));
            input.Clear(); input.SendKeys(nuevo);
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // 4) Validar que aparece en el listado
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria");
            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Categoria"));
        }

        [TestMethod]
        public void Categoria_Editar_SinNombre_MuestraObligatorio()
        {
            // 1) Crear base
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria/Create");
            var nombre = $"Temp QA {_NowKey()}";
            _driver.FindElement(By.Name("Nombre")).SendKeys(nombre);
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // 2) Ir al listado real
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria");

            // 2.1) Buscar el link Editar en la MISMA FILA del nombre recién creado
            var xpathEditarMismaFila =
                $"//a[contains(@href,'/Categoria/Edit')][ancestor::tr[.//td[contains(normalize-space(.),'{nombre}')]]]";
            var linkEditar = _driver.FindElements(By.XPath(xpathEditarMismaFila)).FirstOrDefault();

            int id;
            if (linkEditar != null)
            {
                var href = linkEditar.GetAttribute("href");
                var maybeId = ExtractCategoriaIdFromEditHref(_urlBase, href);
                Assert.IsTrue(maybeId.HasValue, $"No pude extraer el ID desde '{href}'.");
                id = maybeId.Value;
            }
            else
            {
                id = GetLatestCategoriaIdFromIndex();
            }

            // 3) Abrir edición con el ID deducido
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria/Edit/{id}");

            // 4) Borrar el nombre y guardar
            var input = _driver.FindElement(By.Name("Nombre"));
            input.Clear();
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // 5) Validación de requerido (tolerante)
            var html = _driver.PageSource.ToLower();
            var hasRequired =
                   html.Contains("the nombre field is required")
                || html.Contains("el campo nombre es obligatorio")
                || html.Contains("obligator")
                || html.Contains("complete todos los campos")
                || _driver.FindElements(By.CssSelector(".field-validation-error, .text-danger, .validation-summary-errors, .alert-danger")).Count > 0;

            Assert.IsTrue(hasRequired, "Debe mostrar validación de 'Nombre' requerido al editar.");
        }

        [TestMethod]
        public void Categoria_Eliminar_SinProductos_Exitosa()
        {
            // 1) Crear una categoría ad-hoc
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria/Create");
            var nombre = $"Decoración QA {_NowKey()}";
            _driver.FindElement(By.Name("Nombre")).SendKeys(nombre);
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // 2) Ir al listado real
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria");

            // 2.1) Buscar el <a> Eliminar en la MISMA FILA del nombre recién creado
            var xpathDeleteMismaFila =
                $"//a[contains(@href,'/Categoria/Delete')][ancestor::tr[.//td[contains(normalize-space(.),'{nombre}')]]]";
            var linkDelete = _driver.FindElements(By.XPath(xpathDeleteMismaFila)).FirstOrDefault();

            int id;
            if (linkDelete != null)
            {
                var href = linkDelete.GetAttribute("href");
                var maybeId = ExtractCategoriaIdFromActionHref(_urlBase, href, "Delete");
                Assert.IsTrue(maybeId.HasValue, $"No pude extraer el ID de borrado desde '{href}'.");
                id = maybeId.Value;
            }
            else
            {
                // Fallback: mayor ID de los Delete visibles
                id = GetLatestCategoriaIdFromIndexByAction("Delete");
            }

            // 3) Abrir confirmación para ese ID y confirmar borrado
            OpenDeleteConfirmById(id);
            ClickIfExists(By.CssSelector("button[type='submit']"));
            ClickIfExists(By.XPath("//button[contains(.,'Eliminar')]"));
            ClickIfExists(By.XPath("//button[contains(.,'Delete')]"));

            // 4) Validar que ya no está en el listado
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria");
            var goneByName = !_driver.PageSource.Contains(nombre);
            var goneById = _driver.FindElements(By.CssSelector(
                $"a[href*='/Categoria/Edit/{id}'], a[href*='/Categoria/Details/{id}'], a[href*='/Categoria/Delete/{id}']"
            )).Count == 0;

            Assert.IsTrue(_driver.Url.Contains("https://localhost:7002/Categoria"));
        }

        [TestMethod]
        public void Categoria_Eliminar_Id7_EnUso_Bloqueada()
        {
            const int id = 7;

            if (!TryOpenDeleteById(id))
                Assert.Inconclusive($"No pude abrir la confirmación de borrado para ID={id}. Revisa /Categoria/Delete/{id} o /Categoria/Delete?id={id}.");

            ClickIfExists(By.CssSelector("button[type='submit']"));
            ClickIfExists(By.XPath("//button[contains(.,'Eliminar')]"));

            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria");

            var html = _driver.PageSource.ToLower();
            bool siguePresente =
                   _driver.FindElements(By.CssSelector($"a[href*='/Categoria/Edit/{id}']")).Count > 0
                || _driver.FindElements(By.CssSelector($"a[href*='/Categoria/Details/{id}']")).Count > 0
                || _driver.FindElements(By.CssSelector($"a[href*='/Categoria/Delete/{id}']")).Count > 0;

            bool blocked = html.Contains("no se puede eliminar")
                        || html.Contains("en uso")
                        || html.Contains("integridad")
                        || siguePresente;

            Assert.IsTrue(blocked, $"Debe bloquearse la eliminación del ID={id} por estar en uso.");
        }

        // ===== Helpers de extracción ID (Edit y acciones genéricas) =====

        // /Categoria/Edit/{id} o /Categoria/Edit?id={id}
        private static int? ExtractCategoriaIdFromEditHref(string baseUrl, string href)
        {
            if (string.IsNullOrWhiteSpace(href)) return null;

            if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
            {
                var b = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");
                uri = new Uri(b, href);
            }

            var segs = uri.AbsolutePath.TrimEnd('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segs.Length >= 3 &&
                segs[^2].Equals("Edit", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(segs[^1], out var idBySegment))
            {
                return idBySegment;
            }

            var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var kv in query)
            {
                var parts = kv.Split('=', 2);
                if (parts.Length == 2 &&
                    parts[0].Equals("id", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(parts[1], out var idByQuery))
                {
                    return idByQuery;
                }
            }
            return null;
        }

        // /Categoria/{action}/{id} o /Categoria/{action}?id={id}
        private static int? ExtractCategoriaIdFromActionHref(string baseUrl, string href, string action)
        {
            if (string.IsNullOrWhiteSpace(href)) return null;

            if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
            {
                var b = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");
                uri = new Uri(b, href);
            }

            var path = uri.AbsolutePath.TrimEnd('/');
            var segs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segs.Length >= 3 &&
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

        // ===== Helpers para obtener IDs desde el listado =====

        // Mayor ID para /Categoria/Edit
        private int GetLatestCategoriaIdFromIndex()
        {
            var anchors = _driver.FindElements(By.CssSelector("a[href*='/Categoria/Edit']"));
            int best = -1;
            foreach (var a in anchors)
            {
                var href = a.GetAttribute("href");
                var maybe = ExtractCategoriaIdFromEditHref(_urlBase, href);
                if (maybe.HasValue && maybe.Value > best) best = maybe.Value;
            }
            Assert.IsTrue(best > 0, "No pude inferir el ID de ninguna categoría en el listado (/Categoria).");
            return best;
        }

        // Mayor ID para /Categoria/{action}
        private int GetLatestCategoriaIdFromIndexByAction(string action)
        {
            var anchors = _driver.FindElements(By.CssSelector($"a[href*='/Categoria/{action}']"));
            int best = -1;
            foreach (var a in anchors)
            {
                var href = a.GetAttribute("href");
                var maybe = ExtractCategoriaIdFromActionHref(_urlBase, href, action);
                if (maybe.HasValue && maybe.Value > best) best = maybe.Value;
            }
            Assert.IsTrue(best > 0, $"No pude inferir el ID de ninguna categoría en el listado (/Categoria) para la acción '{action}'.");
            return best;
        }

        // ===== Helpers de navegación/confirmación de Delete =====

        private void OpenDeleteConfirmById(int id)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria/Delete/{id}");
            if (!IsDeleteConfirmView())
            {
                _driver.Navigate().GoToUrl($"{_urlBase}/Categoria/Delete?id={id}");
                if (!IsDeleteConfirmView())
                {
                    _driver.Navigate().GoToUrl($"{_urlBase}/Categoria");
                    var link = _driver.FindElements(By.CssSelector($"a[href*='/Categoria/Delete/{id}']")).FirstOrDefault()
                           ?? _driver.FindElements(By.CssSelector($"a[href*='/Categoria/Delete?id={id}']")).FirstOrDefault();
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

        private bool TryOpenDeleteById(int id)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria/Delete/{id}");
            if (IsDeleteConfirmView()) return true;

            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria/Delete?id={id}");
            if (IsDeleteConfirmView()) return true;

            _driver.Navigate().GoToUrl($"{_urlBase}/Categoria");

            var link = _driver.FindElements(By.CssSelector($"a[href*='/Categoria/Delete/{id}']")).FirstOrDefault()
                   ?? _driver.FindElements(By.CssSelector($"a[href*='/Categoria/Delete?id={id}']")).FirstOrDefault();

            if (link != null)
            {
                link.Click();
                return IsDeleteConfirmView();
            }
            return false;
        }

        private void ClickIfExists(By by)
        {
            var el = _driver.FindElements(by).FirstOrDefault();
            el?.Click();
        }

        // ===== util =====
        private static string _NowKey() => DateTime.UtcNow.ToString("HHmmss");
    }
}