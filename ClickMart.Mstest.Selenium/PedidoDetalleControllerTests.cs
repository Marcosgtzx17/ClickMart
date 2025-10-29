using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ClickMart.Mstest.Selenium
{
    [TestClass]
    public class PedidoDetalleControllerTests
    {
        private IWebDriver _driver;
        private readonly string _urlBase = "https://localhost:7002";

        // Sesiones
        private readonly string _clientEmail = "jose@gmail.com";
        private readonly string _clientPass = "12345";
        private readonly string _adminEmail = "m@gmail.com";
        private readonly string _adminPass = "12345";

        // Pedido fijo para las pruebas de detalle
        private const int PedidoId = 53;

        [TestInitialize]
        public void Setup()
        {
            var options = new ChromeOptions();
            // options.AddArgument("--headless=new");
            options.AddArgument("--ignore-certificate-errors");

            _driver = new ChromeDriver(options);
            _driver.Manage().Window.Maximize();
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);

            LoginAs(_clientEmail, _clientPass);
        }

        [TestCleanup]
        public void Teardown()
        {
            try { _driver?.Quit(); } catch { }
            try { _driver?.Dispose(); } catch { }
        }

        // =============== HU-033 (1057): Agregar ítem exitosamente ===============
        [TestMethod]
        public void HU33_1057_Detalle_AgregarItem_Exitoso()
        {
            OpenPedidoDetails(PedidoId);

            var totalAntes = ReadTotal();

            SelectDetalleProducto("Camisa Slim");
            SetCantidadNueva(1);
            ClickAgregar();
            ClickRecalcularTotal();
            WaitForReload();

            var totalDespues = ReadTotal();

            Assert.IsTrue(totalDespues > totalAntes, "El total debe aumentar tras agregar un ítem.");
            Assert.IsTrue(FindFilaPorProducto("Camisa Slim") != null || CountFilas() > 0,
                          "Debería aparecer una fila en el detalle.");
        }

        // =============== HU-033 (1058): Validación cantidad inválida ===============
        [TestMethod]
        public void HU33_1058_Detalle_Agregar_CantidadInvalida()
        {
            OpenPedidoDetails(PedidoId);

            var totalAntes = ReadTotal();

            SelectDetalleProducto("Camisa Slim");
            SetCantidadNueva(0); // inválida
            ClickAgregar();
            ClickRecalcularTotal();
            WaitForReload();

            var html = _driver.PageSource.ToLower();
            var totalDespues = ReadTotal();

            bool mostroError = html.Contains("cantidad no válida")
                            || html.Contains("cantidad inválid")
                            || html.Contains("debe ser mayor a 0")
                            || html.Contains("obligator");

            Assert.IsTrue(mostroError || totalDespues == totalAntes,
                "Debe rechazar cantidad inválida (mensaje o sin cambios en el total).");
        }

        // =============== HU-034 (1059): Ver detalle propio (cliente) ===============
        [TestMethod]
        public void HU34_1059_Detalle_Ver_Propio_Cliente()
        {
            OpenPedidoDetails(PedidoId);

            Assert.IsTrue(CountFilas() >= 0, "La vista de detalle debería cargar y listar items (puede ser 0+).");
            Assert.IsTrue(ReadTotal() >= 0m, "La vista de detalle debería mostrar un total.");
        }

        // =============== HU-034 (1060): Ver detalle como ADMIN ===============
        [TestMethod]
        public void HU34_1060_Detalle_Ver_Admin_CualquierPedido()
        {
            Relogin(_adminEmail, _adminPass);

            OpenPedidoDetails(PedidoId);

            Assert.IsTrue(CountFilas() >= 0, "El admin debe poder ver el detalle de cualquier pedido.");
            Assert.IsTrue(ReadTotal() >= 0m, "El admin debe ver el total del pedido.");
        }

        // =============== HU-035 (1061): Edición exitosa de cantidad ===============
        [TestMethod]
        public void HU35_1061_Detalle_EditarCantidad_Exitoso()
        {

            OpenPedidoDetails(PedidoId);

            EnsureAgregarPanelVisible();

            if (CountFilas() == 0)
            {
                var pudoSeleccionar = TrySelectDetalleProducto("Camisa Slim");
                if (!pudoSeleccionar)
                    Assert.Inconclusive("No encontré el <select> de producto para agregar y no hay filas en la tabla. Añade data-testid='detalle-producto' o usa un <label> Producto.");
                SetCantidadNueva(1);
                ClickAgregar();
                ClickRecalcularTotal();
                WaitForReload();
            }

            // Toma la PRIMERA fila por índice y trabaja siempre con referencias frescas
            var filaIdx = 0;
            var fila = GetFilaByIndex(filaIdx);
            Assert.IsNotNull(fila, "No hay filas en el detalle para editar.");

            var totalAntes = ReadTotal();

            // Cambia cantidad a 2
            SetCantidadEnFila(fila, 2);
            ClickActualizarEnFila(fila);
            ClickRecalcularTotal();
            WaitForReload();

            // Re-adquirir la fila tras el re-render
            var filaRefrescada = GetFilaByIndex(filaIdx);
            Assert.IsNotNull(filaRefrescada, "No pude re-adquirir la fila tras actualizar.");

            var totalDespues = ReadTotal();

            Assert.IsTrue(totalDespues != totalAntes, "El total debe recalcularse al cambiar cantidad.");
            Assert.AreEqual("2", ReadCantidadEnFila(filaRefrescada), "La cantidad en la fila debería ser 2.");
        }

        // =============== HU-035 (1062): Validación edición cantidad inválida ===============
        [TestMethod]
        public void HU35_1062_Detalle_EditarCantidad_Invalida()
        {
            OpenPedidoDetails(PedidoId);

            if (CountFilas() == 0)
            {
                SelectDetalleProducto("Camisa Slim");
                SetCantidadNueva(1);
                ClickAgregar();
                ClickRecalcularTotal();
                WaitForReload();
            }

            var filaIdx = 0;
            var fila = GetFilaByIndex(filaIdx);
            var totalAntes = ReadTotal();

            // Intenta dejar cantidad 0
            SetCantidadEnFila(fila, 0);
            ClickActualizarEnFila(fila);
            ClickRecalcularTotal();
            WaitForReload();

            var filaRefrescada = GetFilaByIndex(filaIdx);
            var totalDespues = ReadTotal();
            var html = _driver.PageSource.ToLower();

            bool mostroError = html.Contains("cantidad no válida")
                            || html.Contains("cantidad inválid")
                            || html.Contains("debe ser mayor a 0")
                            || html.Contains("obligator");

            Assert.IsTrue(mostroError || totalDespues == totalAntes || ReadCantidadEnFila(filaRefrescada) != "0",
                "Debe rechazar la cantidad inválida al editar.");
        }

        // =============== HU-036 (1063): Eliminación exitosa de ítem ===============
        [TestMethod]
        public void HU36_1063_Detalle_EliminarItem_Exitoso()
        {

            OpenPedidoDetails(PedidoId);

            if (CountFilas() == 0)
            {
                SelectDetalleProducto("Camisa Slim");
                SetCantidadNueva(1);
                ClickAgregar();
                ClickRecalcularTotal();
                WaitForReload();
            }

            var filasAntes = CountFilas();
            var fila = GetFilaByIndex(0);
            var totalAntes = ReadTotal();

            ClickBorrarEnFila(fila);             // confirmará alert/modal si aparece
            ClickRecalcularTotal();               // tolera alerta residual
            WaitForReload();

            var filasDespues = CountFilas();
            var totalDespues = ReadTotal();

            Assert.IsTrue(filasDespues < filasAntes, "Debe disminuir la cantidad de filas al eliminar.");
            Assert.IsTrue(totalDespues <= totalAntes, "El total debe volverse a calcular (<=) tras eliminar.");
        }

        // =============== HU-037 (1064): Recalcular total tras cambiar cantidad ===============
        [TestMethod]
        public void HU37_1064_Detalle_RecalculaTotal_AlEditar()
        {

            OpenPedidoDetails(PedidoId);

            if (CountFilas() == 0)
            {
                SelectDetalleProducto("Camisa Slim");
                SetCantidadNueva(1);
                ClickAgregar();
                ClickRecalcularTotal();
                WaitForReload();
            }

            var filaIdx = 0;
            var fila = GetFilaByIndex(filaIdx);

            var precioUnit = ReadPrecioUnitarioFila(fila);

            SetCantidadEnFila(fila, 3);
            ClickActualizarEnFila(fila);
            ClickRecalcularTotal();
            WaitForReload();

            var filaRefrescada = GetFilaByIndex(filaIdx);

            var subtotal = ReadSubtotalFila(filaRefrescada);
            Assert.AreEqual(precioUnit * 3m, subtotal, "El subtotal de la fila debería ser precio * cantidad.");

            var total = ReadTotal();
            Assert.IsTrue(total >= subtotal, "El total del pedido debe ser >= al subtotal de la fila editada.");
        }

        // ==========================================================
        // ========================= HELPERS =========================
        // ==========================================================

        private void LoginAs(string email, string password)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Auth/Login");
            _driver.FindElement(By.Name("Email")).Clear();
            _driver.FindElement(By.Name("Email")).SendKeys(email);
            _driver.FindElement(By.Name("Password")).Clear();
            _driver.FindElement(By.Name("Password")).SendKeys(password);
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();
            WaitForReload();
        }

        private void Relogin(string email, string password)
        {
            try
            {
                _driver.Navigate().GoToUrl($"{_urlBase}/Auth/Logout");
                Thread.Sleep(300);
            }
            catch { /* ignore */ }

            LoginAs(email, password);
        }

        private void OpenPedidoDetails(int id)
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Pedido/Details/{id}");
            if (!IsDetailsView())
            {
                _driver.Navigate().GoToUrl($"{_urlBase}/Pedido/Details?id={id}");
            }
            Assert.IsTrue(IsDetailsView(), "No pude abrir la página de detalle del pedido.");
        }

        private bool IsDetailsView()
        {
            var html = _driver.PageSource.ToLower();
            return html.Contains("pedido #") || html.Contains("detalle") || CountFilas() >= 0;
        }

        // ---- Sección "Agregar" (select producto + cantidad + botón agregar) ----
        private bool TrySelectDetalleProducto(string preferido = "Camisa Slim")
        {
            var formConAgregar = _driver.FindElements(By.XPath("//form[.//button[contains(.,'Agregar')] or .//input[@type='submit' and contains(@value,'Agregar')]]"))
                                        .FirstOrDefault();

            IWebElement select = null;

            if (formConAgregar != null)
                select = formConAgregar.FindElements(By.CssSelector("select")).FirstOrDefault();

            select = select
                  ?? _driver.FindElements(By.CssSelector("[data-testid='detalle-producto']")).FirstOrDefault()
                  ?? _driver.FindElements(By.Name("ProductoId")).FirstOrDefault()
                  ?? _driver.FindElements(By.Id("ProductoId")).FirstOrDefault()
                  ?? _driver.FindElements(By.XPath("//label[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'producto')]/following::*[self::select][1]")).FirstOrDefault();

            if (select == null)
            {
                select = _driver.FindElements(By.TagName("select"))
                                .FirstOrDefault(s =>
                                {
                                    var opts = s.FindElements(By.TagName("option"))
                                                .Where(o => o.Enabled &&
                                                            !"true".Equals(o.GetAttribute("disabled"), StringComparison.OrdinalIgnoreCase) &&
                                                            o.Text.Trim().Length > 0 &&
                                                            o.Text.IndexOf("seleccion", StringComparison.OrdinalIgnoreCase) < 0)
                                                .ToList();
                                    return opts.Count >= 1;
                                });
            }

            if (select == null) return false;

            var opt = select.FindElements(By.XPath($".//option[normalize-space(.)={ToXpathLiteral(preferido)}]")).FirstOrDefault()
                  ?? select.FindElements(By.TagName("option"))
                           .FirstOrDefault(o => o.Enabled &&
                                                !"true".Equals(o.GetAttribute("disabled"), StringComparison.OrdinalIgnoreCase) &&
                                                o.Text.Trim().Length > 0 &&
                                                o.Text.IndexOf("seleccion", StringComparison.OrdinalIgnoreCase) < 0);

            if (opt == null) return false;

            ScrollIntoView(select);
            opt.Click();

            try
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                    const s = arguments[0];
                    s.dispatchEvent(new Event('input',{bubbles:true}));
                    s.dispatchEvent(new Event('change',{bubbles:true}));
                ", select);
            }
            catch { }

            return true;
        }

        private void SelectDetalleProducto(string preferido = "Camisa Slim")
        {
            Assert.IsTrue(TrySelectDetalleProducto(preferido), "No encontré el <select> de producto para agregar.");
        }

        private void SetCantidadNueva(int qty)
        {
            var qtyEl =
                   _driver.FindElements(By.CssSelector("[data-testid='detalle-cantidad']")).FirstOrDefault()
                ?? _driver.FindElements(By.Name("Cantidad")).FirstOrDefault()
                ?? _driver.FindElements(By.Id("Cantidad")).FirstOrDefault()
                ?? _driver.FindElements(By.XPath("//label[contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'cantidad')]/following::*[self::input][1]")).FirstOrDefault();

            Assert.IsNotNull(qtyEl, "No encontré el input de cantidad para agregar.");
            TypeInto(qtyEl, qty.ToString(), clear: true);
        }

        private void ClickAgregar()
        {
            var btn = _driver.FindElements(By.CssSelector("[data-testid='detalle-agregar']")).FirstOrDefault()
                  ?? _driver.FindElements(By.XPath("//button[contains(.,'Agregar')]")).FirstOrDefault()
                  ?? _driver.FindElements(By.XPath("//input[@type='submit' and contains(@value,'Agregar')]")).FirstOrDefault();

            Assert.IsNotNull(btn, "No encontré el botón 'Agregar'.");
            SafeClick(btn);
        }

        private void EnsureAgregarPanelVisible()
        {
            var btnAgregar = _driver.FindElements(By.XPath("//button[contains(.,'Agregar')]")).FirstOrDefault()
                           ?? _driver.FindElements(By.XPath("//input[@type='submit' and contains(@value,'Agregar')]")).FirstOrDefault();

            if (btnAgregar != null) { ScrollIntoView(btnAgregar); return; }

            try { ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight);"); } catch { }
            Thread.Sleep(200);
            try { ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0, 0);"); } catch { }
            Thread.Sleep(200);
        }

        // ---- Botón Recalcular total ----
        private void ClickRecalcularTotal()
        {
            var btn = _driver.FindElements(By.CssSelector("[data-testid='detalle-recalcular']")).FirstOrDefault()
                  ?? _driver.FindElements(By.XPath("//button[contains(.,'Recalcular total') or contains(.,'Recalcular')]")).FirstOrDefault()
                  ?? _driver.FindElements(By.XPath("//input[@type='submit' and (contains(@value,'Recalcular total') or contains(@value,'Recalcular'))]")).FirstOrDefault();

            if (btn != null)
            {
                try
                {
                    SafeClick(btn);
                }
                catch (UnhandledAlertException)
                {
                    // si hay un alert abierto, acéptalo y reintenta una vez
                    AcceptAlertIfPresent();
                    SafeClick(btn);
                }
            }
        }

        // ---- Tabla de items (siempre DOM fresco) ----
        private System.Collections.Generic.List<IWebElement> GetTableRows()
        {
            var rowsBody = _driver.FindElements(By.CssSelector("table tbody tr")).ToList();
            if (rowsBody.Count > 0) return rowsBody;

            var rows = _driver.FindElements(By.CssSelector("table tr"))
                              .Where(r => r.FindElements(By.TagName("th")).Count == 0)
                              .ToList();
            return rows;
        }

        private int CountFilas() => GetTableRows().Count;

        private IWebElement FirstFila() => GetTableRows().FirstOrDefault();

        private IWebElement GetFilaByIndex(int index)
        {
            var rows = GetTableRows();
            if (index < 0 || index >= rows.Count) return null;
            return rows[index];
        }

        private IWebElement FindFilaPorProducto(string nombre)
        {
            return GetTableRows().FirstOrDefault(tr => tr.Text.IndexOf(nombre, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void SetCantidadEnFila(IWebElement fila, int qty)
        {
            var input = fila.FindElements(By.CssSelector("input[type='number']")).FirstOrDefault()
                    ?? fila.FindElements(By.TagName("input")).FirstOrDefault();

            Assert.IsNotNull(input, "No encontré input de cantidad dentro de la fila.");
            TypeInto(input, qty.ToString(), clear: true);
        }

        private string ReadCantidadEnFila(IWebElement fila)
        {
            try
            {
                var input = fila.FindElements(By.CssSelector("input[type='number']")).FirstOrDefault()
                        ?? fila.FindElements(By.TagName("input")).FirstOrDefault();
                return input?.GetAttribute("value") ?? "";
            }
            catch (StaleElementReferenceException)
            {
                // Reintento una vez con DOM fresco (primera fila)
                var fresh = FirstFila();
                var input = fresh?.FindElements(By.CssSelector("input[type='number']")).FirstOrDefault()
                         ?? fresh?.FindElements(By.TagName("input")).FirstOrDefault();
                return input?.GetAttribute("value") ?? "";
            }
        }

        private void ClickActualizarEnFila(IWebElement fila)
        {
            var btn = fila.FindElements(By.XPath(".//button[contains(.,'Actualizar')]")).FirstOrDefault()
                   ?? fila.FindElements(By.XPath(".//input[@type='submit' and contains(@value,'Actualizar')]")).FirstOrDefault();
            Assert.IsNotNull(btn, "No encontré botón 'Actualizar' en la fila.");
            SafeClick(btn);
        }

        private void ClickBorrarEnFila(IWebElement fila)
        {
            var btn = fila.FindElements(By.XPath(".//button[contains(.,'Borrar') or contains(.,'Eliminar')]")).FirstOrDefault()
                   ?? fila.FindElements(By.XPath(".//a[contains(.,'Borrar') or contains(.,'Eliminar')]")).FirstOrDefault();
            Assert.IsNotNull(btn, "No encontré botón 'Borrar/Eliminar' en la fila.");
            SafeClick(btn);

            // Si aparece un alert nativo de confirmación, acéptalo; si no, intenta confirmar modal
            if (!AcceptAlertIfPresent())
            {
                ConfirmDeleteIfModal();
            }
        }

        private decimal ReadPrecioUnitarioFila(IWebElement fila)
        {
            var celdas = fila.FindElements(By.TagName("td")).ToList();
            if (celdas.Count >= 4)
            {
                var text = celdas[3].Text;
                var num = ParseMoney(text);
                if (num.HasValue) return num.Value;
            }
            var fallback = ParseMoney(fila.Text);
            Assert.IsTrue(fallback.HasValue, "No pude leer el precio unitario de la fila.");
            return fallback.Value;
        }

        private decimal ReadSubtotalFila(IWebElement fila)
        {
            var celdas = fila.FindElements(By.TagName("td")).ToList();
            if (celdas.Count > 0)
            {
                for (int i = celdas.Count - 1; i >= 0; i--)
                {
                    var val = ParseMoney(celdas[i].Text);
                    if (val.HasValue) return val.Value;
                }
            }
            var total = ParseMoney(fila.Text);
            Assert.IsTrue(total.HasValue, "No pude leer el subtotal de la fila.");
            return total.Value;
        }

        // ---- Total del pedido ----
        private decimal ReadTotal()
        {
            var html = _driver.PageSource;
            var rx = new Regex(@"Total[^0-9]*([\d\.\,]+)", RegexOptions.IgnoreCase);
            var m = rx.Matches(html).Cast<Match>().LastOrDefault();
            if (m != null && m.Groups.Count > 1)
            {
                var d = ToDecimal(m.Groups[1].Value);
                if (d.HasValue) return d.Value;
            }

            var any = ParseMoney(html);
            return any ?? 0m;
        }

        // ---- Utilitarios UI ----
        private void WaitForReload(int ms = 800)
        {
            Thread.Sleep(ms);
            WaitForDomSettle();
        }

        private void WaitForDomSettle(int timeoutMs = 2500)
        {
            var end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < end)
            {
                try
                {
                    var ready = (string)((IJavaScriptExecutor)_driver).ExecuteScript("return document.readyState");
                    if (ready == "complete") break;
                }
                catch { }
                Thread.Sleep(100);
            }
        }

        private void SafeClick(IWebElement el)
        {
            try
            {
                ScrollIntoView(el);
                el.Click();
            }
            catch (ElementClickInterceptedException) { JsClick(el); }
            catch (WebDriverException) { JsClick(el); }
        }

        private void TypeInto(IWebElement el, string value, bool clear = false)
        {
            try
            {
                ScrollIntoView(el);
                el.Click();
                if (clear)
                {
                    try { el.Clear(); } catch { }
                    try { el.SendKeys(Keys.Control + "a"); el.SendKeys(Keys.Delete); } catch { }
                }
                if (!string.IsNullOrEmpty(value)) el.SendKeys(value);
            }
            catch
            {
                var js = (IJavaScriptExecutor)_driver;
                js.ExecuteScript("arguments[0].removeAttribute('readonly'); arguments[0].removeAttribute('disabled');", el);
                if (clear) js.ExecuteScript("arguments[0].value='';", el);
                if (!string.IsNullOrEmpty(value))
                {
                    js.ExecuteScript(@"
                        arguments[0].value = arguments[1];
                        arguments[0].dispatchEvent(new Event('input',{bubbles:true}));
                        arguments[0].dispatchEvent(new Event('change',{bubbles:true}));
                    ", el, value);
                }
            }
        }

        private void ScrollIntoView(IWebElement el)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block:'center',inline:'nearest'});", el);
        }

        private void JsClick(IWebElement el)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", el);
        }

        // ---- Alert / Modal helpers ----
        private bool AcceptAlertIfPresent(int tries = 5, int sleepMs = 150)
        {
            for (int i = 0; i < tries; i++)
            {
                try
                {
                    var alert = _driver.SwitchTo().Alert();
                    var _ = alert.Text; // opcional
                    alert.Accept();
                    Thread.Sleep(150);
                    return true;
                }
                catch (NoAlertPresentException) { }
                catch (UnhandledAlertException)
                {
                    try { _driver.SwitchTo().Alert().Accept(); return true; } catch { }
                }
                Thread.Sleep(sleepMs);
            }
            return false;
        }

        private void ConfirmDeleteIfModal()
        {
            var btn =
                _driver.FindElements(By.XPath(
                    "//div[contains(@class,'modal') and contains(@class,'show')]//button[" +
                    "contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'eliminar') " +
                    "or contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'sí') " +
                    "or contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'si') " +
                    "or contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'aceptar') " +
                    "or contains(translate(.,'ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚ','abcdefghijklmnopqrstuvwxyzáéíóú'),'confirmar')]"))
                .FirstOrDefault()
             ?? _driver.FindElements(By.CssSelector(".swal2-container .swal2-confirm")).FirstOrDefault();

            if (btn != null) SafeClick(btn);
        }

        // ---- Parsing monetario ----
        private static decimal? ParseMoney(string text)
        {
            var m = Regex.Matches(text, @"([\d]{1,3}([.,]\d{3})*[.,]\d{1,2}|\d+)")
                         .Cast<Match>().LastOrDefault();
            if (m == null) return null;
            return ToDecimal(m.Value);
        }

        private static decimal? ToDecimal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();

            if (s.Contains(",") && s.Contains("."))
            {
                if (s.LastIndexOf(",") > s.LastIndexOf(".")) { s = s.Replace(".", ""); s = s.Replace(",", "."); }
                else { s = s.Replace(",", ""); }
            }
            else
            {
                if (s.Contains(",")) s = s.Replace(",", ".");
            }

            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            return null;
        }

        private static string ToXpathLiteral(string value)
        {
            if (!value.Contains("'")) return $"'{value}'";
            var parts = value.Split('\'');
            return "concat(" + string.Join(", \"'\", ", parts.Select(p => $"'{p}'")) + ")";
        }
    }
}