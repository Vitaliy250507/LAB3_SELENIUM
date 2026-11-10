using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq;

namespace LAB3
{
    [TestFixture]
    public class TheInternetTests
    {
        private IWebDriver driver;
        private WebDriverWait wait;
        private readonly string baseUrl = "https://the-internet.herokuapp.com/";

        [SetUp]
        public void SetUp()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            options.PageLoadStrategy = PageLoadStrategy.Eager;

            options.AddArgument("--disable-notifications");
            options.AddUserProfilePreference("profile.default_content_setting_values.geolocation", 1);

            options.SetLoggingPreference(LogType.Browser, LogLevel.All);

            driver = new ChromeDriver(options);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(15);
            wait = new WebDriverWait(driver, TimeSpan.FromSeconds(7));
        }

        [TearDown]
        public void TearDown()
        {
            driver?.Quit();
            driver?.Dispose();
        }

        [Test]
        public void Test1_AddRemoveElements()
        {
            driver.Navigate().GoToUrl(baseUrl + "add_remove_elements/");

            IWebElement addButton = driver.FindElement(By.XPath("//button[text()='Add Element']"));
            addButton.Click();

            IWebElement deleteButton = wait.Until(d => d.FindElement(By.ClassName("added-manually")));
            Assert.IsTrue(deleteButton.Displayed, "Кнопка Delete не з'явилася");

            deleteButton.Click();
            Assert.AreEqual(0, driver.FindElements(By.ClassName("added-manually")).Count, "Кнопка Delete не зникла");
        }

        [Test]
        public void Test2_Checkboxes()
        {
            driver.Navigate().GoToUrl(baseUrl + "checkboxes");

            var checkboxes = driver.FindElements(By.CssSelector("input[type='checkbox']"));
            IWebElement checkbox1 = checkboxes[0];
            IWebElement checkbox2 = checkboxes[1];

            if (!checkbox1.Selected) checkbox1.Click();
            Assert.IsTrue(checkbox1.Selected, "Чекбокс 1 не був обраний");

            if (checkbox2.Selected) checkbox2.Click();
            Assert.IsFalse(checkbox2.Selected, "Чекбокс 2 залишився обраним");
        }

        [Test]
        public void Test3_Dropdown()
        {
            driver.Navigate().GoToUrl(baseUrl + "dropdown");

            IWebElement dropdownElement = driver.FindElement(By.Id("dropdown"));
            SelectElement select = new SelectElement(dropdownElement);

            select.SelectByText("Option 1");
            Assert.AreEqual("Option 1", select.SelectedOption.Text, "Опція 1 не була вибрана");

            select.SelectByValue("2");
            Assert.AreEqual("Option 2", select.SelectedOption.Text, "Опція 2 не була вибрана");
        }

        [Test]
        public void Test4_Inputs()
        {
            driver.Navigate().GoToUrl(baseUrl + "inputs");

            IWebElement inputField = driver.FindElement(By.CssSelector("input[type='number']"));

            string testValue = "12345";
            inputField.SendKeys(testValue);

            Assert.AreEqual(testValue, inputField.GetAttribute("value"), "Введене значення не співпадає");
        }

        [Test]
        public void Test5_StatusCodes()
        {
            driver.Navigate().GoToUrl(baseUrl + "status_codes");

            driver.FindElement(By.XPath("//a[contains(@href, 'status_codes/200')]")).Click();

            IWebElement content = wait.Until(d => d.FindElement(By.Id("content")));
            Assert.IsTrue(content.Text.Contains("This page returned a 200 status code"), "Неправильний статус код.");
        }



        [Test]
        public void Test6_DragAndDrop()
        {
            driver.Navigate().GoToUrl(baseUrl + "drag_and_drop");

            ((IJavaScriptExecutor)driver).ExecuteScript(@"
                const dt = new DataTransfer();
                const src = document.querySelector('#column-a');
                const dst = document.querySelector('#column-b');
                const fire = (el, type) => el.dispatchEvent(
                    new DragEvent(type, { bubbles: true, cancelable: true, dataTransfer: dt }));
                fire(src, 'dragstart'); fire(dst, 'dragenter'); fire(dst, 'dragover');
                fire(dst, 'drop');      fire(src, 'dragend');");

            Assert.AreEqual("B", driver.FindElement(By.CssSelector("#column-a header")).Text, "Колонка A не змінилась.");
            Assert.AreEqual("A", driver.FindElement(By.CssSelector("#column-b header")).Text, "Колонка B не змінилась.");
        }
        [Test]
        public void Test7_ShiftingContent()
        {
            driver.Navigate().GoToUrl(baseUrl + "shifting_content/menu");

            var menuItems = driver.FindElements(By.CssSelector("li a"));
            Assert.Greater(menuItems.Count, 0, "Меню пусте або не знайдене.");

            Assert.AreEqual("Home", menuItems[0].Text, "Перший елемент не Home.");
        }

        [Test]
        public void Test8_Geolocation()
        {
            var chromeDriver = (ChromeDriver)driver;

            chromeDriver.ExecuteCdpCommand("Browser.grantPermissions", new Dictionary<string, object>
            {
                { "origin", "https://the-internet.herokuapp.com" },
                { "permissions", new[] { "geolocation" } }
            });
            chromeDriver.ExecuteCdpCommand("Emulation.setGeolocationOverride", new Dictionary<string, object>
            {
                { "latitude", 50.4501 }, { "longitude", 30.5234 }, { "accuracy", 100 }
            });

            driver.Navigate().GoToUrl(baseUrl + "geolocation");
            driver.FindElement(By.XPath("//button[text()='Where am I?']")).Click();

            IWebElement latValue = wait.Until(d => {
                var el = d.FindElements(By.Id("lat-value")).FirstOrDefault();
                return el != null && !string.IsNullOrEmpty(el.Text) ? el : null;
            });
            IWebElement longValue = driver.FindElement(By.Id("long-value"));

            StringAssert.StartsWith("50.45", latValue.Text, "Широта не відповідає заданій.");
            StringAssert.StartsWith("30.52", longValue.Text, "Довгота не відповідає заданій.");
        }

        [Test]
        public void Test9_JavaScriptErrors()
        {
            driver.Navigate().GoToUrl(baseUrl + "javascript_error");

            System.Threading.Thread.Sleep(1000);

            var browserLogs = driver.Manage().Logs.GetLog(LogType.Browser);

            bool hasJsError = browserLogs.Any(log =>
                log.Level == LogLevel.Severe ||
                log.Message.Contains("TypeError") ||
                log.Message.Contains("undefined"));

            Assert.IsTrue(hasJsError, "Очікувана JavaScript помилка не знайдена в логах браузера.");
        }

        [Test]
        public void Test10_ExitIntent()
        {
            driver.Navigate().GoToUrl(baseUrl + "exit_intent");

            Thread.Sleep(1000); 

            ((IJavaScriptExecutor)driver).ExecuteScript(
                "document.documentElement.dispatchEvent(new MouseEvent('mouseleave', { clientY: -5, bubbles: true }));");

            IWebElement modal = wait.Until(d =>
            {
                var el = d.FindElement(By.Id("ouibounce-modal"));
                return el.Displayed ? el : null;
            });

            Assert.IsTrue(modal.Displayed, "Модальне вікно не з'явилося.");
            StringAssert.Contains("modal window", modal.Text.ToLower(), $"Текст модального вікна не збігається. Фактичний: '{modal.Text}'");
        }
    }
}