using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;

namespace ClickMart.Mstest.Selenium
{
    [TestClass]
    public class AuthControllerTests
    {
        private IWebDriver _driver;
        private readonly string _urlBase = "https://localhost:7002";

        // Credenciales conocidas
        private readonly string _adminEmail = "m@gmail.com";
        private readonly string _adminPass = "12345";
        private readonly string _clientEmail = "jose@gmail.com";
        private readonly string _clientPass = "12345";

        [TestInitialize]
        public void Setup()
        {
            var options = new ChromeOptions();
            // options.AddArgument("--headless=new");
            options.AddArgument("--ignore-certificate-errors");
            _driver = new ChromeDriver(options);
            _driver.Manage().Window.Maximize();
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }

        [TestCleanup]
        public void Teardown()
        {
            try { _driver?.Quit(); } catch { }
            try { _driver?.Dispose(); } catch { }
        }

        // ================= HU1: REGISTRO =================

        // HU1 (1001): Registro exitoso de visitante
        [TestMethod]
        public void HU1_1001_Registro_Exitoso()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Auth/Registrar");

            _driver.FindElement(By.Name("Nombre")).SendKeys("QA Visitor");
            _driver.FindElement(By.Name("Email")).SendKeys($"qa+{DateTime.UtcNow:yyyyMMddHHmmssfff}@mailinator.com");
            _driver.FindElement(By.Name("Telefono")).SendKeys("77777777");
            _driver.FindElement(By.Name("Direccion")).SendKeys("Av. QA 123");
            _driver.FindElement(By.Name("Password")).SendKeys("Password123!");

            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            // Esperado: permitir ingresar al sitio (redirige fuera de /Registrar o muestra confirmación)
            Assert.IsFalse(_driver.Url.Contains("/Auth/Registrar"));
            StringAssert.StartsWith(_driver.Url, _urlBase);
        }

        // HU1 (1002): Registro con correo existente
        [TestMethod]
        public void HU1_1002_Registro_CorreoExistente_MuestraError()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Auth/Registrar");

            _driver.FindElement(By.Name("Nombre")).SendKeys("Usuario Existente");
            _driver.FindElement(By.Name("Email")).SendKeys(_clientEmail); // correo ya registrado
            _driver.FindElement(By.Name("Telefono")).SendKeys("77777777");
            _driver.FindElement(By.Name("Direccion")).SendKeys("Av. QA 123");
            _driver.FindElement(By.Name("Password")).SendKeys("Password123!");

            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            var html = _driver.PageSource.ToLower();
            Assert.IsTrue(
                html.Contains("ya est") || html.Contains("registrad") || html.Contains("correo ya existe") ||
                _driver.Url.Contains("/Auth/Registrar"),
                "Debería bloquear el registro y mostrar mensaje de 'email ya está registrado'."
            );
        }

        // HU1 (1003): Registro con campos vacíos
        [TestMethod]
        public void HU1_1003_Registro_CamposVacios_MuestraObligatorios()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Auth/Registrar");
            // Enviar el form vacío
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            var html = _driver.PageSource.ToLower();
            Assert.IsTrue(
                html.Contains("obligator") || html.Contains("complete todos los campos") ||
                _driver.FindElements(By.CssSelector(".field-validation-error, .text-danger, .validation-summary-errors, .alert-danger")).Count > 0,
                "Debe mostrar validación de campos obligatorios."
            );
        }

        // ================= HU2: LOGIN =================

        // HU2 (1004): Inicio de sesión exitoso (cliente)
        [TestMethod]
        public void HU2_1004_Login_Cliente_Exitoso()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Auth/Login");

            _driver.FindElement(By.Name("Email")).SendKeys(_clientEmail);
            _driver.FindElement(By.Name("Password")).SendKeys(_clientPass);
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            Assert.IsFalse(_driver.Url.Contains("/Auth/Login"));
            StringAssert.StartsWith(_driver.Url, _urlBase);
        }

        // HU2 (1005): Credenciales inválidas
        [TestMethod]
        public void HU2_1005_Login_CredencialesInvalidas_MuestraError()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Auth/Login");

            _driver.FindElement(By.Name("Email")).SendKeys(_clientEmail);
            _driver.FindElement(By.Name("Password")).SendKeys("Clave_Equivocada_XXX!");
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            var html = _driver.PageSource.ToLower();
            Assert.IsTrue(html.Contains("incorrect") || html.Contains("inválid") || html.Contains("credencial"));
        }

        // HU2 (1006): Campos vacíos
        [TestMethod]
        public void HU2_1006_Login_CamposVacios_MuestraObligatorios()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Auth/Login");
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            var html = _driver.PageSource.ToLower();
            Assert.IsTrue(
                html.Contains("obligatorio") || html.Contains("complete todos los campos") ||
                _driver.FindElements(By.CssSelector(".field-validation-error, .text-danger, .validation-summary-errors, .alert-danger")).Count > 0
            );
        }

        // Extra: Login Admin (sanity de rol)
        [TestMethod]
        public void Login_Admin_MuestraMenuAdministracion()
        {
            _driver.Navigate().GoToUrl($"{_urlBase}/Auth/Login");

            _driver.FindElement(By.Name("Email")).SendKeys(_adminEmail);
            _driver.FindElement(By.Name("Password")).SendKeys(_adminPass);
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();

            var html = _driver.PageSource;
            Assert.IsTrue(
                html.Contains("Usuarios") || html.Contains("Categorías") || html.Contains("Distribuidores"),
                "Deberían verse opciones del menú de administración."
            );
        }
    }
}