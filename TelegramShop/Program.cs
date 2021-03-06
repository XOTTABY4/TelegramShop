﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// MySql
using System.Data.Common;
using MySql.Data.MySqlClient;

using System.Configuration; // для взятия строки подключения из App.config

// Telegram API
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.Enums; // для получения типа сообщения
using Telegram.Bot.Types.ReplyMarkups; // для создания клавиатуры
using System.Text.RegularExpressions;
using Telegram;
using System.IO;
using System.Net;
using System.Data.SqlClient;

namespace TelegramShop
{
    class Program
    {
        static TelegramBotClient Bot;

        static List<Product> products = new List<Product>();
        static bool isDouble;
        static double minPrice = 0;
        static int productsInCartCount = 0;

        static string emailLogin;
        static int confirmationСode; // код подтвержения почты: 5 цифр
        static string passwordLogin;

        static User user = new User();

        static User newUser = new User();

        static ProductOrder productOrder = new ProductOrder();
        static ProductOrder productOrderForNewUser = new ProductOrder();

        static Dictionary<int, Product> messageIdProductPairs = new Dictionary<int, Product>();

        static List<ProductDetail> currentOrder = new List<ProductDetail>();

        static Cart cart = new Cart();

        // последнее сообщение, посланное ботом для идентификации на какой вопрос отвечает пользователь
        static string lastMessage;

        static string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

        static List<ProductDetail> productDetails = null;

        static public List<string> GetAllCategories()
        {
            List<string> categories = new List<string>();

            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;

                    sqlConnection.Open();

                    MySqlCommand sqlCommand = sqlConnection.CreateCommand();
                    sqlCommand.CommandText = "SELECT name FROM category;";
                    sqlCommand.Connection = sqlConnection;

                    using (DbDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(reader["name"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //categories.Add(ex.Message);
            }

            return categories;
        }

        static public int GetCategoryIdByName(string categoryName)
        {
            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;

                    sqlConnection.Open();

                    MySqlCommand sqlCommand = sqlConnection.CreateCommand();
                    // Избавиться от возможного SQL_Injection
                    sqlCommand.CommandText = $"SELECT id FROM category WHERE name=\"{categoryName}\" LIMIT 1;";
                    sqlCommand.Connection = sqlConnection;

                    using (DbDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            return Int32.Parse(reader["id"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return -1; // ошибка -- нет такой категории
        }

        static public List<Product> GetAllProductsByCategoryId(int categoryId)
        {
            List<Product> products = new List<Product>();

            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;

                    sqlConnection.Open();

                    MySqlCommand sqlCommand = sqlConnection.CreateCommand();
                    // Избавиться от возможного SQL_Injection
                    sqlCommand.CommandText = $"SELECT * FROM product WHERE category_id={categoryId};";
                    sqlCommand.Connection = sqlConnection;

                    using (DbDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product(Int32.Parse(reader["id"].ToString()), reader["name"].ToString(), Int32.Parse(reader["category_id"].ToString()), Int32.Parse(reader["code"].ToString()), double.Parse(reader["price"].ToString()), Int32.Parse(reader["availability"].ToString()), reader["brand"].ToString(), reader["description"].ToString(), Int32.Parse(reader["is_new"].ToString()), Int32.Parse(reader["is_recommended"].ToString()), Int32.Parse(reader["status"].ToString())));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return products;
        }

        static public List<Product> GetAllProducts()
        {
            List<Product> products = new List<Product>();

            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;

                    sqlConnection.Open();

                    MySqlCommand sqlCommand = sqlConnection.CreateCommand();
                    // Избавиться от возможного SQL_Injection
                    sqlCommand.CommandText = $"SELECT * FROM product;";
                    sqlCommand.Connection = sqlConnection;

                    using (DbDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product(Int32.Parse(reader["id"].ToString()), reader["name"].ToString(), Int32.Parse(reader["category_id"].ToString()), Int32.Parse(reader["code"].ToString()), double.Parse(reader["price"].ToString()), Int32.Parse(reader["availability"].ToString()), reader["brand"].ToString(), reader["description"].ToString(), Int32.Parse(reader["is_new"].ToString()), Int32.Parse(reader["is_recommended"].ToString()), Int32.Parse(reader["status"].ToString())));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return products;
        }

        static public List<Product> GetAllProductsByCategoryName(string categoryName)
        {
            int categoryId = GetCategoryIdByName(categoryName);

            if (categoryId <= 0) return null;

            return GetAllProductsByCategoryId(categoryId);
        }

        static public List<Product> GetAllProductsBetweenPrices(double minPrice = 0, double maxPrice = 9999999999999)
        {
            List<Product> products = new List<Product>();

            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;

                    sqlConnection.Open();

                    MySqlCommand sqlCommand = sqlConnection.CreateCommand();
                    // Избавиться от возможного SQL_Injection
                    sqlCommand.CommandText = $"SELECT * FROM product WHERE price BETWEEN {minPrice} AND {maxPrice};";
                    sqlCommand.Connection = sqlConnection;

                    using (DbDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product(Int32.Parse(reader["id"].ToString()), reader["name"].ToString(), Int32.Parse(reader["category_id"].ToString()), Int32.Parse(reader["code"].ToString()), double.Parse(reader["price"].ToString()), Int32.Parse(reader["availability"].ToString()), reader["brand"].ToString(), reader["description"].ToString(), Int32.Parse(reader["is_new"].ToString()), Int32.Parse(reader["is_recommended"].ToString()), Int32.Parse(reader["status"].ToString())));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return products;
        }

        static void Main(string[] args)
        {
            try
            {
                string API_token = System.Configuration.ConfigurationManager.AppSettings["TelegramBot_API_Token"];
                // создать клиента бота на основе токена, который даёт Botfather
                // при создании бота
                Bot = new TelegramBotClient(API_token);

                // подписка на событие -- когда будет приходить сообщение,
                // будет вызываться метод BotOnMessageReceived
                Bot.OnMessage += BotOnMessageReceived;

                // а это для InlineKeyboardButton.WithCallbackData("Пункт 2")
                // при нажатии на кнопку "Пункт 2" будет срабатывать метод BonOnCallbackReceived
                Bot.OnCallbackQuery += BonOnCallbackReceived;

                // выдаст имя бота
                var me = Bot.GetMeAsync().Result;

                Console.WriteLine(me.FirstName); // название бота: "EShop777bot"
                                                 // начать получение сообщений
                Bot.StartReceiving();

                Console.ReadLine();

                // остановить получение сообщений
                Bot.StopReceiving();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static string MinifyHTML(string html)
        {
            string html2;
            while ((html2 = Regex.Replace(html, @"[ \t\r\n\f][ \t\r\n\f]", " ", RegexOptions.Singleline)).CompareTo(html) != 0)
            {
                html = string.Copy(html2);
            }

            while ((html2 = Regex.Replace(html, @"> <", "><", RegexOptions.Singleline)).CompareTo(html) != 0)
            {
                html = string.Copy(html2);
            }

            return html2;
        }

        private static async void BonOnCallbackReceived(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            string buttonText = e.CallbackQuery.Data;

            // для отладки
            //string name = $"{e.CallbackQuery.From.FirstName} {e.CallbackQuery.From.LastName}";
            //Console.WriteLine($"{name} нажал кнопку {buttonText}");

            var products = GetAllProductsByCategoryName(buttonText);

            for (int i = 0; i < products?.Count; ++i)
            {
                SendImageAndDescriptionOfProduct(products[i], e.CallbackQuery.From.Id);
            }

            // значит нажата кнопка, не содержащая название категории товара
            if (products == null)
            {
                switch (buttonText)
                {
                    case "🔍 Поиск":
                        // отправка текста пользователю ( у каждого пользователя свой отдельный чат с ботом )
                        // message.From.Id -- Id чата
                        InlineKeyboardButton nameButton = InlineKeyboardButton.WithCallbackData("Часть названия");
                        InlineKeyboardButton priceButton = InlineKeyboardButton.WithCallbackData("Цена");
                        InlineKeyboardButton codeButton = InlineKeyboardButton.WithCallbackData("Код");
                        List<List<InlineKeyboardButton>> categoriesGroupsOfButtons = new List<List<InlineKeyboardButton>>();
                        categoriesGroupsOfButtons.Add(new List<InlineKeyboardButton>(new[] { nameButton }));
                        categoriesGroupsOfButtons.Add(new List<InlineKeyboardButton>(new[] { priceButton }));
                        categoriesGroupsOfButtons.Add(new List<InlineKeyboardButton>(new[] { codeButton }));
                        var catalogInlineKeyboard = new InlineKeyboardMarkup(categoriesGroupsOfButtons);
                        try
                        {
                            // отправка клавиатуры в чат пользователю
                            await Bot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Выберите критерий поиска товара:", replyMarkup: catalogInlineKeyboard);
                            lastMessage = "Выберите критерий поиска товара:";
                        }
                        catch
                        { }

                        break;
                    case "Цена":
                        await Bot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Цена товара от (грн):");
                        lastMessage = "Цена товара от (грн):";
                        break;
                    case "Часть названия":
                        await Bot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Название товара содержит:");
                        lastMessage = "Название товара содержит:";
                        break;
                    case "Код":
                        await Bot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Часть или весь код товара:");
                        lastMessage = "Часть или весь код товара:";
                        break;
                    case "В корзину":

                        // Нужно добавить товар в корзину, который захотели поместить в корзину!
                        // И радом с кнопкой "🛒 Корзина" обновить количество в ней товара "🛒 Корзина (3)" -- вот так, например

                        ++productsInCartCount;
                        ShowMenu(e.CallbackQuery.From.Id, productsInCartCount);

                        var p = messageIdProductPairs[e.CallbackQuery.Message.MessageId];

                        cart.Add(new ProductDetail { Count = 1, Product = p });

                        //lastMessage = "Укажите количество:";
                        break;

                    case "▶":

                        p = messageIdProductPairs[e.CallbackQuery.Message.MessageId];

                        var productsInCart = cart.GetProductDetails();

                        ProductDetail nextProduct = null;

                        if (productsInCart.Count > cart.ProductIndexInCart(p) + 1)
                            nextProduct = productsInCart[cart.ProductIndexInCart(p) + 1];

                        if (nextProduct == null) return;

                        var ImageUrl = $@"C:\\ospanel\\domains\\eshop\\upload\\images\\products\\{nextProduct.Product.id}.jpg";

                        string txt = $"{productsInCart[0].Product.name}\nЦена: {nextProduct.Product.price} грн.\nПодробнее: https://scehlov.000webhostapp.com/product/{nextProduct.Product.id}";

                        SendImageAndTextWithoutButtonInCart(e.CallbackQuery.From.Id, ImageUrl, txt, nextProduct, cart.ProductIndexInCart(p) + 1);

                        // удаление сообщения
                        await Bot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);

                        break;

                    case "◀":

                        p = messageIdProductPairs[e.CallbackQuery.Message.MessageId];

                        productsInCart = cart.GetProductDetails();

                        ProductDetail previousProduct = null;

                        if (0 <= cart.ProductIndexInCart(p) - 1)
                            previousProduct = productsInCart[cart.ProductIndexInCart(p) - 1];

                        if (previousProduct == null) return;

                        ImageUrl = $@"C:\\ospanel\\domains\\eshop\\upload\\images\\products\\{previousProduct.Product.id}.jpg";

                        txt = $"{productsInCart[0].Product.name}\nЦена: {previousProduct.Product.price} грн.\nПодробнее: https://scehlov.000webhostapp.com/product/{previousProduct.Product.id}";

                        SendImageAndTextWithoutButtonInCart(e.CallbackQuery.From.Id, ImageUrl, txt, previousProduct, cart.ProductIndexInCart(p) - 1);

                        // удаление сообщения
                        await Bot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);

                        break;

                    case "🔺":

                        p = messageIdProductPairs[e.CallbackQuery.Message.MessageId];

                        productsInCart = cart.GetProductDetails();

                        int index = cart.ProductIndexInCart(p);

                        var currentProductDetail = productsInCart[index];

                        cart.IncrementProductCount(currentProductDetail);

                        ImageUrl = $@"C:\\ospanel\\domains\\eshop\\upload\\images\\products\\{currentProductDetail.Product.id}.jpg";

                        txt = $"{productsInCart[0].Product.name}\nЦена: {currentProductDetail.Product.price} грн.\nПодробнее: https://scehlov.000webhostapp.com/product/{currentProductDetail.Product.id}";

                        SendImageAndTextWithoutButtonInCart(e.CallbackQuery.From.Id, ImageUrl, txt, currentProductDetail, index);

                        // удаление сообщения
                        await Bot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);

                        break;

                    case "🔻":

                        p = messageIdProductPairs[e.CallbackQuery.Message.MessageId];

                        productsInCart = cart.GetProductDetails();

                        index = cart.ProductIndexInCart(p);

                        currentProductDetail = productsInCart[index];

                        cart.DecrementProductCount(currentProductDetail);

                        ImageUrl = $@"C:\\ospanel\\domains\\eshop\\upload\\images\\products\\{currentProductDetail.Product.id}.jpg";

                        txt = $"{productsInCart[0].Product.name}\nЦена: {currentProductDetail.Product.price} грн.\nПодробнее: https://scehlov.000webhostapp.com/product/{currentProductDetail.Product.id}";

                        SendImageAndTextWithoutButtonInCart(e.CallbackQuery.From.Id, ImageUrl, txt, currentProductDetail, index);

                        // удаление сообщения
                        await Bot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);

                        break;

                    case "❌":

                        // если в корзине это единственный товар, то показать сообщение что в корзине нет товаров, 
                        // иначе если это не последний товар, то показать следующий товар,
                        // иначе показать предыдущий товар

                        p = messageIdProductPairs[e.CallbackQuery.Message.MessageId];

                        productsInCart = cart.GetProductDetails();

                        int currentProductIndex = cart.ProductIndexInCart(p);

                        currentProductDetail = productsInCart[currentProductIndex];

                        cart.DeleteProductDetailByProductDetail(currentProductDetail);

                        ShowMenu(e.CallbackQuery.From.Id, cart.GetProductsCount());

                        if (productsInCart.Count == 0)
                        {
                            await Bot.SendTextMessageAsync(e.CallbackQuery.From.Id, "В корзине пусто... 😭");
                            // удаление сообщения
                            await Bot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        }
                        else if (productsInCart.Count == 1)
                        {
                            nextProduct = productsInCart[0];

                            ImageUrl = $@"C:\\ospanel\\domains\\eshop\\upload\\images\\products\\{nextProduct.Product.id}.jpg";

                            txt = $"{nextProduct.Product.name}\nЦена: {nextProduct.Product.price} грн.\nПодробнее: https://scehlov.000webhostapp.com/product/{nextProduct.Product.id}";

                            SendImageAndTextWithoutButtonInCart(e.CallbackQuery.From.Id, ImageUrl, txt, nextProduct, 0);

                            // удаление сообщения
                            await Bot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        }
                        // товар не последний
                        else if (currentProductIndex < productsInCart.Count - 1)
                        {
                            nextProduct = productsInCart[currentProductIndex + 1];

                            ImageUrl = $@"C:\\ospanel\\domains\\eshop\\upload\\images\\products\\{nextProduct.Product.id}.jpg";

                            txt = $"{nextProduct.Product.name}\nЦена: {nextProduct.Product.price} грн.\nПодробнее: https://scehlov.000webhostapp.com/product/{nextProduct.Product.id}";

                            SendImageAndTextWithoutButtonInCart(e.CallbackQuery.From.Id, ImageUrl, txt, nextProduct, currentProductIndex);

                            // удаление сообщения
                            await Bot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        }
                        // данный товар -- последний
                        else
                        {
                            previousProduct = productsInCart[currentProductIndex - 1];

                            ImageUrl = $@"C:\\ospanel\\domains\\eshop\\upload\\images\\products\\{previousProduct.Product.id}.jpg";

                            txt = $"{previousProduct.Product.name}\nЦена: {previousProduct.Product.price} грн.\nПодробнее: https://scehlov.000webhostapp.com/product/{previousProduct.Product.id}";

                            SendImageAndTextWithoutButtonInCart(e.CallbackQuery.From.Id, ImageUrl, txt, previousProduct, currentProductIndex - 1);

                            // удаление сообщения
                            await Bot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        }

                        break;

                    case "✅ Да":

                        await Bot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Введите email:");
                        lastMessage = "Введите email:";

                        break;

                    case "❌ Нет":

                        await Bot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Регистрация для заказа обязательна\nВведите имя:");
                        lastMessage = "Регистрация для заказа обязательна\nВведите имя:";

                        break;

                    default:
                        if (buttonText.Contains("✅ Заказ на"))
                        {
                            InlineKeyboardButton yesButton = InlineKeyboardButton.WithCallbackData("✅ Да");
                            InlineKeyboardButton noButton = InlineKeyboardButton.WithCallbackData("❌ Нет");

                            List<List<InlineKeyboardButton>> answerGroupsOfButtons = new List<List<InlineKeyboardButton>>();

                            answerGroupsOfButtons.Add(new List<InlineKeyboardButton>(new[] { yesButton, noButton }));

                            var answerInlineKeyboard = new InlineKeyboardMarkup(answerGroupsOfButtons);

                            var message = await Bot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Вы зарегистрированы на сайте?", replyMarkup: answerInlineKeyboard);
                            lastMessage = "Вы зарегистрированы на сайте?";
                        }

                        break;
                }
            }
        }

        private static User IsGoodLoginData(string emailLogin, string passwordLogin)
        {
            // подключение к БД и проверка есть ли такие данные в таблице user

            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;

                    sqlConnection.Open();

                    MySqlCommand sqlCommand = sqlConnection.CreateCommand();
                    // Избавиться от возможного SQL_Injection
                    sqlCommand.CommandText = $"SELECT * FROM user WHERE email=\"{emailLogin}\" AND password=\"{passwordLogin}\" LIMIT 1;";
                    sqlCommand.Connection = sqlConnection;

                    using (DbDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            user = new User(Int32.Parse(reader["id"].ToString()), reader["name"].ToString(), reader["email"].ToString(), reader["password"].ToString(), reader["role"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //categories.Add(ex.Message);
            }

            return user;
        }

        private static string WrapTextByHtmlTemplate(string text)
        {
            return "<!DOCTYPE html><html lang='en'><head><meta charset='UTF-8'><title></title></head><body>" + text + "</body></html>";
        }

        public static async void SendImageAndTextWithInCartButton(int chatId, string ImageUrl, string text, Product product)
        {
            InlineKeyboardButton putToCartButton;

            putToCartButton = InlineKeyboardButton.WithCallbackData("В корзину");

            List<List<InlineKeyboardButton>> categoriesGroupsOfButtons = new List<List<InlineKeyboardButton>>();

            categoriesGroupsOfButtons.Add(new List<InlineKeyboardButton>(new[] { putToCartButton }));
            var catalogInlineKeyboard = new InlineKeyboardMarkup(categoriesGroupsOfButtons);

            using (var stream = System.IO.File.Open(ImageUrl, FileMode.Open))
            {
                string fileName = ImageUrl.Split('\\').Last();
                var message = await Bot.SendPhotoAsync(chatId, new InputOnlineFile(stream, fileName), text, ParseMode.Html, replyMarkup: catalogInlineKeyboard);

                // добавляем подробности о сообщении для идентификации товара в сообщении -- для идентификации какой товар хочет поместить в корзину пользователь
                messageIdProductPairs.Add(message.MessageId, product);
            }
        }

        public static async void SendImageAndText(int chatId, string ImageUrl, string text, Product product)
        {
            using (var stream = System.IO.File.Open(ImageUrl, FileMode.Open))
            {
                string fileName = ImageUrl.Split('\\').Last();
                var message = await Bot.SendPhotoAsync(chatId, new InputOnlineFile(stream, fileName), text, ParseMode.Html);

                // добавляем подробности о сообщении для идентификации товара в сообщении -- для идентификации какой товар хочет поместить в корзину пользователь
                messageIdProductPairs.Add(message.MessageId, product);
            }
        }

        public static async void SendImageAndTextWithoutButtonInCart(int chatId, string ImageUrl, string text, ProductDetail productDetail, int productDetailPosition)
        {
            InlineKeyboardButton deleteFromCartButton = InlineKeyboardButton.WithCallbackData("❌");
            InlineKeyboardButton putOnOneMoreToCartButton = InlineKeyboardButton.WithCallbackData("🔺");
            InlineKeyboardButton countInCartButton = InlineKeyboardButton.WithCallbackData(productDetail.Count.ToString() + " шт.");
            InlineKeyboardButton putOnOffMoreToCartButton = InlineKeyboardButton.WithCallbackData("🔻");

            InlineKeyboardButton prevProductInCartButton = InlineKeyboardButton.WithCallbackData("◀");
            InlineKeyboardButton positionOfProductInCartButton = InlineKeyboardButton.WithCallbackData((productDetailPosition + 1) + "/" + cart.GetProductDetails().Count());
            InlineKeyboardButton nextProductInCartButton = InlineKeyboardButton.WithCallbackData("▶");

            InlineKeyboardButton confirmOrderButton = InlineKeyboardButton.WithCallbackData($"✅ Заказ на {cart.GetProductsTotalPrice(cart.GetProductDetails())} грн. Оформить?");

            List<List<InlineKeyboardButton>> categoriesGroupsOfButtons = new List<List<InlineKeyboardButton>>();

            categoriesGroupsOfButtons.Add(new List<InlineKeyboardButton>(new[] { deleteFromCartButton, putOnOneMoreToCartButton, countInCartButton, putOnOffMoreToCartButton }));

            categoriesGroupsOfButtons.Add(new List<InlineKeyboardButton>(new[] { prevProductInCartButton, positionOfProductInCartButton, nextProductInCartButton }));

            categoriesGroupsOfButtons.Add(new List<InlineKeyboardButton>(new[] { confirmOrderButton }));

            var catalogInlineKeyboard = new InlineKeyboardMarkup(categoriesGroupsOfButtons);

            try
            {
                using (var stream = System.IO.File.Open(ImageUrl, FileMode.Open))
                {
                    string fileName = ImageUrl.Split('\\').Last();
                    var message = await Bot.SendPhotoAsync(chatId, new InputOnlineFile(stream, fileName), text, ParseMode.Html, replyMarkup: catalogInlineKeyboard);

                    // добавляем подробности о сообщении для идентификации товара в сообщении -- для идентификации какой товар хочет поместить в корзину пользователь
                    messageIdProductPairs.Add(message.MessageId, productDetail.Product);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //public static List<ProductDetail> GetProductDetailsByOrder(ProductOrder productOrder)
        //{
        //    return productOrder.Products;
        //}

        public static async void SendImageAndTextWithoutButtonInOrders(int chatId, string ImageUrl, string text, ProductDetail productDetail, int productDetailPosition)
        {
            InlineKeyboardButton countInOrderButton = InlineKeyboardButton.WithCallbackData(productDetail.Count.ToString() + " шт.");

            InlineKeyboardButton prevProductInOrderButton = InlineKeyboardButton.WithCallbackData("◀");
            InlineKeyboardButton positionOfProductInOrderButton = InlineKeyboardButton.WithCallbackData((productDetailPosition + 1) + "/" + cart.GetProductDetails().Count());
            InlineKeyboardButton nextProductInOrderButton = InlineKeyboardButton.WithCallbackData("▶");

            InlineKeyboardButton confirmOrderButton = InlineKeyboardButton.WithCallbackData($"Заказ на {cart.GetProductsTotalPrice(currentOrder)} грн.");

            List<List<InlineKeyboardButton>> categoriesGroupsOfButtons = new List<List<InlineKeyboardButton>>();

            categoriesGroupsOfButtons.Add(new List<InlineKeyboardButton>(new[] { countInOrderButton }));

            categoriesGroupsOfButtons.Add(new List<InlineKeyboardButton>(new[] { prevProductInOrderButton, positionOfProductInOrderButton, nextProductInOrderButton }));

            categoriesGroupsOfButtons.Add(new List<InlineKeyboardButton>(new[] { confirmOrderButton }));

            var catalogInlineKeyboard = new InlineKeyboardMarkup(categoriesGroupsOfButtons);

            try
            {
                using (var stream = System.IO.File.Open(ImageUrl, FileMode.Open))
                {
                    string fileName = ImageUrl.Split('\\').Last();
                    var message = await Bot.SendPhotoAsync(chatId, new InputOnlineFile(stream, fileName), text, ParseMode.Html, replyMarkup: catalogInlineKeyboard);

                    // добавляем подробности о сообщении для идентификации товара в сообщении -- для идентификации какой товар хочет поместить в корзину пользователь
                    messageIdProductPairs.Add(message.MessageId, productDetail.Product);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void SendImageAndDescriptionOfProduct(Product product, int chatId)
        {
            var ImageUrl = $@"C:\\ospanel\\domains\\eshop\\upload\\images\\products\\{product.id}.jpg";
            string text = $"{product.name}\nЦена: {product.price} грн.\nПодробнее: https://scehlov.000webhostapp.com/product/{product.id}";

            SendImageAndTextWithInCartButton(chatId, ImageUrl, text, product);
        }

        public static async void ShowMenu(int chatId, int productsInCart)
        {
            string cartButtonText = "🛒 Корзина";
            if (productsInCart > 0) cartButtonText += $" ({productsInCart})";

            var replyKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            new KeyboardButton("📁 Каталог"),
                            new KeyboardButton(cartButtonText)
                        },
                        new[]
                        {
                            new KeyboardButton("📦 Заказы"),
                            new KeyboardButton("📢 Новости")
                        },
                        //new[]
                        //{
                        //    new KeyboardButton("⚙ Настройки"),
                        //    new KeyboardButton("❓ Помощь")
                        //},
                        //new[]
                        //{
                        //    new KeyboardButton("☎ Мой Контакт") { RequestContact = true },
                        //    new KeyboardButton("📌 Моя Геолокация") { RequestLocation = true }
                        //},
                        new[]
                        {
                            new KeyboardButton("🌍 Наши магазины на карте (Харьков)")// { RequestContact = true },
                        }
                    });
            var message = await Bot.SendTextMessageAsync(chatId, "Меню", replyMarkup: replyKeyboard);

            // удаление сообщения
            // await Bot.DeleteMessageAsync(chatId, message.MessageId);

            lastMessage = "Меню";
        }

        /*
                 static public List<Product> GetAllProducts()
        {
            List<Product> products = new List<Product>();

            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;

                    sqlConnection.Open();

                    MySqlCommand sqlCommand = sqlConnection.CreateCommand();
                    // Избавиться от возможного SQL_Injection
                    sqlCommand.CommandText = $"SELECT * FROM product;";
                    sqlCommand.Connection = sqlConnection;

                    using (DbDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product(Int32.Parse(reader["id"].ToString()), reader["name"].ToString(), Int32.Parse(reader["category_id"].ToString()), Int32.Parse(reader["code"].ToString()), double.Parse(reader["price"].ToString()), Int32.Parse(reader["availability"].ToString()), reader["brand"].ToString(), reader["description"].ToString(), Int32.Parse(reader["is_new"].ToString()), Int32.Parse(reader["is_recommended"].ToString()), Int32.Parse(reader["status"].ToString())));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return products;
        }
             */

        private static List<ProductOrder> GetAllOrdersFromUser(User user)
        {
            List<ProductOrder> productOrders = new List<ProductOrder>();

            // ищем все заказы от пользователя по его Id -- поле user_id таблицы product_order
            // user.Id
            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;

                    sqlConnection.Open();

                    MySqlCommand sqlCommand = sqlConnection.CreateCommand();
                    // Избавиться от возможного SQL_Injection
                    sqlCommand.CommandText = $"SELECT * FROM product_order WHERE user_id={user.Id};";
                    sqlCommand.Connection = sqlConnection;

                    using (DbDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            //                   int id, string user_name, string user_phone, string user_comment, int user_id, DateTime date, string products, int status)
                            productOrders.Add(new ProductOrder(Int32.Parse(reader["id"].ToString()), reader["user_name"].ToString(), reader["user_phone"].ToString(), reader["user_comment"].ToString(), Int32.Parse(reader["user_id"].ToString()),
                                DateTime.Parse(reader["date"].ToString()), // ???
                                reader["products"].ToString(), Int32.Parse(reader["status"].ToString())));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return productOrders;
        }

        public static List<List<ProductDetail>> GetProductDetailsOfAllUserOrders(List<ProductOrder> productOrders)
        {
            productDetails = new List<ProductDetail>();

            List<List<ProductDetail>> listOfProductDetails = new List<List<ProductDetail>>();

            // парсим значение поля products, имеющее формат JSON: {"35":1,"32":1} в объекты класса ProductDetail (ссылка на товар + кол-во)

            foreach (var productOrder in productOrders)
            {
                string products = productOrder.Products;

                productDetails = ParseProductDetailFromProducts(products);

                listOfProductDetails.Add(productDetails);
            }

            return listOfProductDetails;
        }

        private static List<ProductDetail> ParseProductDetailFromProducts(string products)
        {
            List<ProductDetail> productDetails = new List<ProductDetail>();

            // {"35":1,"32":1}

            StringBuilder sb = new StringBuilder(products);
            // Удаляем первый и последний символы -- фигурные скобки -- ГОВНОКОД
            sb = new StringBuilder(sb.ToString().Substring(1, sb.Length - 2));

            string[] productDetailsStrings = sb.ToString().Split(',');

            // "35":1
            foreach (string productDetailString in productDetailsStrings)
            {
                string[] productAndCount = sb.ToString().Split(':');

                string product = productAndCount[0];
                string productIdString = product.Substring(1, product.Length - 2);
                int productId = Int32.Parse(productIdString);
                string countString = productAndCount[1];
                int count = Int32.Parse(countString);

                productDetails.Add(new ProductDetail(GetProductById(productId), count));
            }

            return productDetails;
        }

        //  async -- асинхронная обработка получаемых сообщений
        // можно одновременно получать и обрабатывать сообщения от разных пользователей
        private static async void BotOnMessageReceived(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var message = e.Message;

            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;

            // если тип сообщения -- не текст, выходим из метода
            if (message == null || message.Type != MessageType.Text)
                return;

            if (message.Text.Contains("🛒 Корзина"))
                message.Text = "🛒 Корзина";

            switch (message.Text)
            {
                case "/start":
                    string text =
    "Добро пожаловать в наш Telegram-магазин\nженского белья 'EShop'!\n" +
@"   Список команд:
/start - запуск бота
/keyboard - вывод клавиатуры";

                    // отправка текста пользователю ( у каждого пользователя свой отдельный чат с ботом )
                    // message.From.Id -- Id чата
                    await Bot.SendTextMessageAsync(message.From.Id, text);
                    lastMessage = text;
                    break;

                // клавиатура
                case "/keyboard":
                    ShowMenu(message.From.Id, productsInCartCount);
                    break;

                case "📦 Заказы":

                    if (user.Email == null)
                    {
                        await Bot.SendTextMessageAsync(message.From.Id, "Для просмотра заказов авторизуйтесь!");
                        return;
                    }

                    List<ProductOrder> productOrders = GetAllOrdersFromUser(user);
                    List<List<ProductDetail>> productDetails = GetProductDetailsOfAllUserOrders(productOrders);

                    foreach (List<ProductDetail> productsInOrder in productDetails)
                    {
                        var ImageUrl2 = $@"C:\\ospanel\\domains\\eshop\\upload\\images\\products\\{productsInOrder[0].Product.id}.jpg";

                        string txt2 = $"{productsInOrder[0].Product.name}\nЦена: {productsInOrder[0].Product.price} грн.\nПодробнее: https://scehlov.000webhostapp.com/product/{productsInOrder[0].Product.id}";

                        currentOrder = productsInOrder;

                        SendImageAndTextWithoutButtonInOrders(message.From.Id, ImageUrl2, txt2, productsInOrder[0], 0);
                    }

                    break;

                case "📁 Каталог":
                    // отправка текста пользователю ( у каждого пользователя свой отдельный чат с ботом )
                    // message.From.Id -- Id чата
                    await Bot.SendTextMessageAsync(message.From.Id, "Каталог");
                    lastMessage = "Каталог";

                    List<string> categories = GetAllCategories();

                    List<List<InlineKeyboardButton>> categoriesGroupsOfButtons = new List<List<InlineKeyboardButton>>();

                    InlineKeyboardButton searchButton = InlineKeyboardButton.WithCallbackData("🔍 Поиск");
                    categoriesGroupsOfButtons.Add(new List<InlineKeyboardButton>(new[] { searchButton }));

                    foreach (string category in categories)
                    {
                        InlineKeyboardButton button = InlineKeyboardButton.WithCallbackData(category);

                        // в каждой группе кнопок будет по одной кнопке -- чтоб по горизонтали была одна кнопка и
                        // текст в ней отображался полностью (название категории товаров)
                        categoriesGroupsOfButtons.Add(new List<InlineKeyboardButton>(new[] { button }));
                    }

                    // массив кнопок меню
                    var catalogInlineKeyboard = new InlineKeyboardMarkup(categoriesGroupsOfButtons);

                    try
                    {
                        // отправка клавиатуры в чат пользователю
                        await Bot.SendTextMessageAsync(message.From.Id, "Выберите раздел, чтобы вывести список товаров:", replyMarkup: catalogInlineKeyboard);
                        lastMessage = "Выберите раздел, чтобы вывести список товаров:";
                    }
                    catch
                    { }

                    break;

                case "🌍 Наши магазины на карте (Харьков)":

                    //await Bot.SendLocationAsync(message.From.Id, latitude: 49.993698f, longitude: 36.231924f);
                    SendImageAndText(message.From.Id, "Map.jpg", "<a href='https://scehlov.000webhostapp.com/shops/'>Наши магазины</a>", null);
                    break;

                case "🛒 Корзина":
                    //await Bot.SendTextMessageAsync(message.Chat.Id, "КОРЗИНА!");

                    /*
                    string info = "";

                    foreach (var item in cart.GetProductDetails())
                    {
                        info += item.Product.name + ": " + item.Count + "\n";
                    }
                    
                    await Bot.SendTextMessageAsync(message.From.Id, info);
                    */

                    var productsInCart = cart.GetProductDetails();

                    var ImageUrl = $@"C:\\ospanel\\domains\\eshop\\upload\\images\\products\\{productsInCart[0].Product.id}.jpg";

                    string txt = $"{productsInCart[0].Product.name}\nЦена: {productsInCart[0].Product.price} грн.\nПодробнее: https://scehlov.000webhostapp.com/product/{productsInCart[0].Product.id}";

                    SendImageAndTextWithoutButtonInCart(message.From.Id, ImageUrl, txt, productsInCart[0], 0);

                    break;

                default:
                    // остальные сообщения

                    switch (lastMessage)
                    {
                        case "Регистрация для заказа обязательна\nВведите имя:":

                            if (message.Text != "Регистрация для заказа обязательна\nВведите имя:")
                            {
                                newUser.Name = message.Text;
                                await Bot.SendTextMessageAsync(message.From.Id, "Введите email: ");
                                lastMessage = "Введите email: ";
                            }

                            break;

                        // для незарегистрированного пользователя
                        case "Введите email: ":

                            if (message.Text != "Введите email:")
                            {
                                newUser.Email = message.Text;

                                // проверка правильности формата адреса почты

                                bool isEmailValid = EmailChecker.IsValidEmailAddress(newUser.Email);

                                if (isEmailValid == false)
                                {
                                    await Bot.SendTextMessageAsync(message.From.Id, "Введите email: ");
                                    lastMessage = "Введите email: ";
                                    break;
                                }

                                // генерация кода подтверждения
                                Random r = new Random();
                                confirmationСode = r.Next(10000, 100000);

                                // отправка на мыло кода подтверждения
                                Email email = new Email("nislacker@gmail.com", "coolprogrammer555", newUser.Email, $"EShop. Подтвердите Email", $"Вас приветствует магазин женской одежды EShop!\nДля подтверждения своей эл. почты, введите в Telegram-магазине код подтверждения: {confirmationСode}");
                                email.Send();

                                await Bot.SendTextMessageAsync(message.From.Id, "Введите код подтверждения Email (он выслан на указанный email): ");
                                lastMessage = "Введите код подтверждения Email (он выслан на указанный email): ";
                            }

                            break;

                        // для незарегистрированного пользователя
                        case "Введите код подтверждения Email (он выслан на указанный email): ":

                            int userCode;

                            if ((Int32.TryParse(message.Text, out userCode) == false) || (confirmationСode != userCode))
                            {
                                await Bot.SendTextMessageAsync(message.From.Id, "Введите код подтверждения Email (он выслан на указанный email): ");
                                lastMessage = "Введите код подтверждения Email (он выслан на указанный email): ";
                                break;
                            }

                            await Bot.SendTextMessageAsync(message.From.Id, "Введите пароль: ");
                            lastMessage = "Введите пароль: ";

                            break;

                        // для незарегистрированного пользователя
                        case "Введите пароль: ":

                            if (message.Text != "Введите пароль: ")
                            {
                                newUser.Password = message.Text;

                                CreateNewUserRecord(newUser);

                                await Bot.SendTextMessageAsync(message.From.Id, "Введите свой телефон: ");
                                lastMessage = "Введите свой телефон: ";
                            }

                            break;

                        // для зарегистрированного пользователя
                        case "Введите email:":

                            if (message.Text != "Введите email:")
                            {
                                emailLogin = message.Text;

                                // проверка правильности формата адреса почты

                                bool isEmailValid = EmailChecker.IsValidEmailAddress(emailLogin);

                                if (isEmailValid == false)
                                {
                                    await Bot.SendTextMessageAsync(message.From.Id, "Введите email:");
                                    lastMessage = "Введите email:";
                                    break;
                                }

                                //

                                await Bot.SendTextMessageAsync(message.From.Id, "Введите пароль:");
                                lastMessage = "Введите пароль:";
                            }

                            break;

                        // для зарегистрированного пользователя
                        case "Введите пароль:":

                            if (message.Text != "Введите пароль:")
                            {
                                passwordLogin = message.Text;

                                if (IsGoodLoginData(emailLogin, passwordLogin) != null)
                                {
                                    /* id
                                     * user_name
                                     * user_phone
                                     * user_comment
                                     * user_id
                                     * date
                                     * products
                                     * status
                                     */

                                    await Bot.SendTextMessageAsync(message.From.Id, "Введите свой телефон:");
                                    lastMessage = "Введите свой телефон:";

                                    //var replyKeyboard = new ReplyKeyboardMarkup(new[]
                                    //{
                                    //    new KeyboardButton("☎ Мой Контакт") { RequestContact = true },
                                    //    new KeyboardButton("Отмена")
                                    //});

                                    //message = await Bot.SendTextMessageAsync(message.From.Id, "Меню");//, replyMarkup: replyKeyboard);
                                }
                                else
                                {
                                    await Bot.SendTextMessageAsync(message.From.Id, "Введите email:");
                                    lastMessage = "Введите email:";
                                }
                            }

                            break;

                        case "Введите свой телефон:":

                            if ((message.Text != "Введите свой телефон:") && (message.Text != "Отмена"))
                            {
                                productOrder.User_phone = message.Text;
                            }

                            await Bot.SendTextMessageAsync(message.From.Id, "Комментарий:");
                            lastMessage = "Комментарий:";

                            break;

                        // для незарегистрированного пользователя
                        case "Введите свой телефон: ":

                            if ((message.Text != "Введите свой телефон: ") && (message.Text != "Отмена"))
                            {
                                productOrderForNewUser.User_phone = message.Text;
                            }

                            await Bot.SendTextMessageAsync(message.From.Id, "Комментарий: ");
                            lastMessage = "Комментарий: ";

                            break;

                        case "Комментарий:":

                            if ((message.Text != "Комментарий:") && (message.Text != "Отмена"))
                            {
                                productOrder.User_comment = message.Text;
                            }

                            CreateProductOrderRecord(productOrder, user);

                            break;

                        // для незарегистрированного пользователя
                        case "Комментарий: ":

                            if ((message.Text != "Комментарий: ") && (message.Text != "Отмена"))
                            {
                                productOrderForNewUser.User_comment = message.Text;
                            }


                            CreateProductOrderRecord(productOrderForNewUser, newUser);

                            break;

                        case "Название товара содержит:":

                            // отобразить все товары с именем, содержащим часть, введенную пользователем
                            products = GetAllProductsByName(message.Text);

                            for (int i = 0; i < products?.Count; ++i)
                            {
                                SendImageAndDescriptionOfProduct(products[i], message.From.Id);
                            }

                            break;

                        case "Цена товара от (грн):":

                            // меняем точки на запятые
                            message.Text = Regex.Replace(message.Text, @"\.", ",");

                            isDouble = Double.TryParse(message.Text, out minPrice);

                            if (isDouble)
                            {
                                // !!! получить введенное от пользователя число -- цену товара для фильтрации запроса
                                await Bot.SendTextMessageAsync(message.Chat.Id, "Цена товара до (грн) -- введите 0 для неограниченной сверху цены:");
                                lastMessage = "Цена товара до (грн) -- введите 0 для неограниченной сверху цены:";
                            }
                            else
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, "Цена товара должна быть числом!");
                            }

                            break;

                        case "Цена товара до (грн) -- введите 0 для неограниченной сверху цены:":

                            double maxPrice;
                            // меняем точки на запятые
                            message.Text = Regex.Replace(message.Text, @"\.", ",");

                            isDouble = Double.TryParse(message.Text, out maxPrice);

                            if (isDouble)
                            {
                                // !!! получить введенное от пользователя число -- цену товара для фильтрации запроса
                                await Bot.SendTextMessageAsync(message.Chat.Id, $"Товары в данном диапазоне цен:");
                                lastMessage = "Товары в данном диапазоне цен:";

                                if (maxPrice == 0) maxPrice = 9999999999999;

                                // отобразить все товары сценой до или равной введенной пользователем
                                products = GetAllProductsBetweenPrices(minPrice, maxPrice);

                                for (int i = 0; i < products?.Count; ++i)
                                {
                                    SendImageAndDescriptionOfProduct(products[i], message.From.Id);
                                }
                            }
                            else
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, "Цена товара должна быть числом!");
                            }
                            break;

                        case "Часть или весь код товара:":

                            int code;
                            bool isInt32 = Int32.TryParse(message.Text, out code);

                            if (isInt32)
                            {
                                products = GetProductsByPartOfCode(code);

                                foreach (Product product in products)
                                {
                                    SendImageAndDescriptionOfProduct(product, message.From.Id);
                                }
                            }
                            else
                            {
                                await Bot.SendTextMessageAsync(message.Chat.Id, "Код товара должен быть числом!");
                            }

                            break;

                        default:
                            break;
                    }

                    break;
            }
        }

        public static void CreateNewUserRecord(User user)
        {
            user.Role = "user";

            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;

                    sqlConnection.Open();

                    // Оператор SQL
                    string sql = string.Format("INSERT INTO user (name, email, password, role) VALUES (@name, @email, @password, @role);");

                    using (MySqlCommand cmd = new MySqlCommand(sql, sqlConnection))
                    {
                        // Добавить параметры
                        cmd.Parameters.AddWithValue("@name", user.Name);
                        cmd.Parameters.AddWithValue("@email", user.Email);
                        cmd.Parameters.AddWithValue("@password", user.Password);
                        cmd.Parameters.AddWithValue("@role", user.Role);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //categories.Add(ex.Message);
            }

            //////
            ///
            // Найти Id созданного юзера и присвоить его же сущности (объекту класса User, представляющий из себя запись в таблице user)
            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;

                    sqlConnection.Open();

                    MySqlCommand sqlCommand = sqlConnection.CreateCommand();
                    sqlCommand.CommandText = $"SELECT id FROM user WHERE name=\"{user.Name}\" LIMIT 1;";
                    sqlCommand.Connection = sqlConnection;

                    using (DbDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            user.Id = Int32.Parse(reader["id"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //categories.Add(ex.Message);
            }
        }

        public static void SendOrderInfoToUserEmail(User user, long orderId, DateTime dateTime)
        {
            Email email = new Email("nislacker@gmail.com", "coolprogrammer555", user.Email, $"EShop. Заказ {orderId} оформлен!", $"Здравствуйте, {user.Name}!\nВас приветствует магазин женской одежды EShop!\nЗаказ, сделанный Вами в Telegram-магазине {dateTime.ToString("dd.MM.yyyy, в HH:mm")}, успешно сохранен и в скором времени будет обработан.\nБлагорадрим за покупки!");
            email.Send();
        }

        private static void CreateProductOrderRecord(ProductOrder productOrder, User user)
        {
            productOrder.Date = DateTime.Now;

            // {"35":1,"32":1}

            StringBuilder res = new StringBuilder("{");

            foreach (ProductDetail pd in cart.GetProductDetails())
            {
                res.Append($"\"{pd.Product.id}\":{pd.Count},");
            }

            if (res.Length > 1)
                res[res.Length - 1] = '}';
            else
                res.Append('}');

            productOrder.Products = res.ToString();
            productOrder.Status = 1;

            // для зарегистрированного пользователя
            if (user.Name == null)
            {
                // email должен быть уникальным для каждого пользователя!
                user = IsGoodLoginData(user.Email, user.Password);
            }
            else
            {
                productOrder.User_id = user.Id;
                productOrder.User_name = user.Name;
            }

            try
            {
                long orderId;

                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;

                    sqlConnection.Open();

                    // Оператор SQL
                    string sql = string.Format("INSERT INTO product_order (user_name, user_phone, user_comment, user_id, date, products, status) VALUES (@User_name, @User_phone, @User_comment, @User_id, @Date, @Products, @Status);");

                    using (MySqlCommand cmd = new MySqlCommand(sql, sqlConnection))
                    {
                        // Добавить параметры
                        cmd.Parameters.AddWithValue("@User_name", productOrder.User_name);
                        cmd.Parameters.AddWithValue("@User_phone", productOrder.User_phone);
                        cmd.Parameters.AddWithValue("@User_comment", productOrder.User_comment);
                        cmd.Parameters.AddWithValue("@User_id", productOrder.User_id);
                        cmd.Parameters.AddWithValue("@Date", productOrder.Date);
                        cmd.Parameters.AddWithValue("@Products", productOrder.Products);
                        cmd.Parameters.AddWithValue("@Status", productOrder.Status);

                        cmd.ExecuteNonQuery();
                        orderId = cmd.LastInsertedId;
                    }
                }

                SendOrderInfoToUserEmail(user, orderId, productOrder.Date);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //categories.Add(ex.Message);
            }
        }

        private static Product GetProductById(int id)
        {
            Product product = null;

            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;
                    sqlConnection.Open();

                    MySqlCommand sqlCommand = sqlConnection.CreateCommand();
                    // Избавиться от возможного SQL_Injection
                    sqlCommand.CommandText = $"SELECT * FROM product WHERE id={id} LIMIT 1;"; // ???
                    sqlCommand.Connection = sqlConnection;

                    using (DbDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            product = new Product(Int32.Parse(reader["id"].ToString()), reader["name"].ToString(), Int32.Parse(reader["category_id"].ToString()), Int32.Parse(reader["code"].ToString()), double.Parse(reader["price"].ToString()), Int32.Parse(reader["availability"].ToString()), reader["brand"].ToString(), reader["description"].ToString(), Int32.Parse(reader["is_new"].ToString()), Int32.Parse(reader["is_recommended"].ToString()), Int32.Parse(reader["status"].ToString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return product;
        }

        private static Product GetProductByCode(int code)
        {
            Product product = null;

            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;
                    sqlConnection.Open();

                    MySqlCommand sqlCommand = sqlConnection.CreateCommand();
                    // Избавиться от возможного SQL_Injection
                    sqlCommand.CommandText = $"SELECT * FROM product WHERE code={code} LIMIT 1;"; // ???
                    sqlCommand.Connection = sqlConnection;

                    using (DbDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            product = new Product(Int32.Parse(reader["id"].ToString()), reader["name"].ToString(), Int32.Parse(reader["category_id"].ToString()), Int32.Parse(reader["code"].ToString()), double.Parse(reader["price"].ToString()), Int32.Parse(reader["availability"].ToString()), reader["brand"].ToString(), reader["description"].ToString(), Int32.Parse(reader["is_new"].ToString()), Int32.Parse(reader["is_recommended"].ToString()), Int32.Parse(reader["status"].ToString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return product;
        }

        private static List<Product> GetProductsByPartOfCode(int partOfCode)
        {
            List<Product> allProducts = GetAllProducts();
            List<Product> findedProducts = new List<Product>();

            foreach (Product product in allProducts)
            {
                if (product.code.ToString().Contains(partOfCode.ToString()))
                {
                    findedProducts.Add(product);
                }
            }

            return findedProducts;
        }

        private static List<Product> GetAllProductsByName(string text)
        {
            List<Product> products = new List<Product>();

            try
            {
                using (MySqlConnection sqlConnection = new MySqlConnection())
                {
                    sqlConnection.ConnectionString = connectionString;

                    sqlConnection.Open();

                    MySqlCommand sqlCommand = sqlConnection.CreateCommand();
                    // Избавиться от возможного SQL_Injection
                    sqlCommand.CommandText = $"SELECT * FROM product WHERE name LIKE '%{text}%';";
                    sqlCommand.Connection = sqlConnection;

                    using (DbDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product(Int32.Parse(reader["id"].ToString()), reader["name"].ToString(), Int32.Parse(reader["category_id"].ToString()), Int32.Parse(reader["code"].ToString()), double.Parse(reader["price"].ToString()), Int32.Parse(reader["availability"].ToString()), reader["brand"].ToString(), reader["description"].ToString(), Int32.Parse(reader["is_new"].ToString()), Int32.Parse(reader["is_recommended"].ToString()), Int32.Parse(reader["status"].ToString())));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return products;
        }
    }
}