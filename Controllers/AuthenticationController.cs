﻿
using AuthenticationSystem;
using AuthSystem.IServices;
using AuthSystem.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using MimeKit.Encodings;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace AuthSystem.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _httpclient;

        public AuthenticationController(HttpClient httpclient, ApplicationDbContext db, IEmailService emailService, IWebHostEnvironment env)
        {
            _db = db;
            _emailService = emailService;
            _env = env;
            _httpclient = httpclient;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(AuthModel auth)
        {
            if (validate(auth))
            {
                try
                {
                    var path = HttpContext.Request.Host;
                    //Uri url = new Uri(path.ToString());


                    auth.VerificationToken = Guid.NewGuid().ToString();
                    _db.Auths.Add(auth);
                    _db.SaveChanges();

                    var subject = "confirm email";
                    byte[] emailbyte = Encoding.UTF8.GetBytes(auth.Email);
                    string encodeemail = Convert.ToBase64String(emailbyte);
                    byte[] tokenbyte = Encoding.UTF8.GetBytes(auth.VerificationToken);
                    string encodetoken = Convert.ToBase64String(tokenbyte);
                    byte[] datebyte = Encoding.UTF8.GetBytes(auth.CreatedAt.Add(TimeSpan.FromMinutes(10)).ToString());
                    string encodedate = Convert.ToBase64String(datebyte);
                    var htmlcontent = "<div>" +
                        "<p>Link is Valid for 10 hour<p>" +
                        $"<a href='{path}/Authentication/EmailVerify?eid={encodeemail.Replace("=", "")}&token={encodetoken.Replace("=", "")}&ed={encodedate.Replace("=", "")}'>Click To Verify Email</a>" +
                        "</div>" +
                        $"https://{path}/Authentication/EmailVerify?eid={encodeemail}&token={encodetoken}&ed={encodedate}";
                    var plaintext = "Email is vailed for 10 minutes";
                    _emailService.SendEmail(auth.FirstName + auth.LastName, auth.Email, subject, plaintext, htmlcontent);
                    TempData["RegisterSuccess"] = "Email Sent Successfully";
                    return View("Register", new AuthModel());
                }
                catch (Exception e)
                {
                    TempData["Err"] = "Something went wrong pleas try again later";
                    _db.Auths.Remove(auth);
                    _db.SaveChanges();
                    return View();
                }

            }
            return View("Register");
        }

        public bool validate(AuthModel auth)
        {
            if (auth != null)
            {
                if (!string.IsNullOrEmpty(auth.FirstName))
                {
                    if (!checkstr(3, 12, auth.FirstName))
                    {
                        TempData["FirstNameErr"] = "Length Must be between 2 to 12 characters";
                        return false;
                    }
                    if (!chaeckTypeNumber(auth.FirstName))
                    {
                        TempData["FirstNameErr"] = "Dont use numbers as first name";
                        return false;
                    }
                }
                else
                {
                    TempData["FirstNameErr"] = "Fields cannot be empty";
                    return false;
                }
                if (!string.IsNullOrEmpty(auth.LastName))
                {
                    if (!checkstr(3, 12, auth.LastName))
                    {
                        TempData["LastNameErr"] = "Length Must be between 2 to 12 characters";
                        return false;
                    }
                    if (!chaeckTypeNumber(auth.LastName))
                    {
                        TempData["LastNameErr"] = "Dont use numbers as last name";
                        return false;
                    }
                }
                else
                {
                    TempData["LastNameErr"] = "Fields cannot be empty";
                    return false;
                }

                if (!string.IsNullOrEmpty(auth.Username))
                {
                    if (!checkstr(3, 12, auth.Username))
                    {
                        TempData["UsernameErr"] = "Length Must be between 2 to 12 characters";
                        return false;
                    }

                }
                else
                {
                    TempData["UsernameErr"] = "Fields cannot be empty";
                    return false;
                }

                if (!string.IsNullOrEmpty(auth.Email))
                {
                    //check for email
                    var pattern = @"^[a-zA-Z0-9]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$";
                    if (!Regex.IsMatch(auth.Email, pattern))
                    {
                        TempData["EmailErr"] = "Please enter the valid Email";
                        return false;
                    }

                }
                else
                {
                    TempData["EmailErr"] = "Fields cannot be empty";
                    return false;
                }

                if (!string.IsNullOrEmpty(auth.Password))
                {
                    if (!checkstr(5, 12, auth.Password))
                    {
                        TempData["PasswordErr"] = "Length Must be between 5 to 12 characters";
                        return false;
                    }

                }
                else
                {
                    TempData["PasswordErr"] = "Fields cannot be empty";
                    return false;
                }

            }
            bool chaeckTypeNumber(string input)
            {
                if (input.Any(char.IsDigit))
                {
                    return false;
                }
                return true;
            }
            bool checkstr(int min, int max, string input)
            {
                if (input.Length > min && input.Length < max)
                {
                    return true;
                }

                return false;
            }
            return true;
        }

        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Login(AuthModel auth)
        {
            if (string.IsNullOrEmpty(auth.Username) || string.IsNullOrEmpty(auth.Password))
            {
                TempData["LoginErr"] = "Fields Cannot Be empty";
                return View();
            }
            else
            {
                var user = _db.Auths.FirstOrDefault(user => user.Username == auth.Username && user.Password == auth.Password);
                if (user != null)
                {
                    TempData["SucessfullLogin"] = "Login Successfull";
                    HttpContext.Session.SetString("Username", user.Username);
                    return RedirectToAction("Dashboard", "Dashboard");
                }
                else
                {
                    TempData["LoginErr"] = "Check Credintials";
                    return View();
                }
            }

        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("Username");
            return View("Login");
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult EmailVerify()
        {
            var query = Request.QueryString.ToUriComponent();
            NameValueCollection param = HttpUtility.ParseQueryString(query);
            var eid = param["eid"];
            var token = param["token"];
            var ed = param["ed"];
            byte[] emailbyte = Convert.FromBase64String(param["eid"]);
            string decodedemail = Encoding.UTF8.GetString(emailbyte);
            byte[] tokenbyte = Convert.FromBase64String(param["token"]);
            string decodedtoken = Encoding.UTF8.GetString(tokenbyte);
            byte[] datebyte = Convert.FromBase64String(param["ed"]);
            string decodeddate = Encoding.UTF8.GetString(datebyte);

            //https://localhost:44321/Authentication/EmailVerify?eid=Y29kZXJhbWVldEBnbWFpbC5jb20=&token=ZjIwODBmMWEtMWM3MS00NGIwLTg4ZWYtZGRlYTgxNGNkZjA4&ed=OC8xMi8yMDIzIDExOjUyOjEzIFBN

            if (Convert.ToDateTime(decodeddate) > DateTime.Now)
            {
                //verify the email by updating the database
                var user = _db.Auths.FirstOrDefault(user => user.Email == decodedemail);
                if (user != null)
                {
                    if (!user.Is_Active)
                    {
                        user.Is_Active = true;
                        _db.Auths.Update(user);
                        _db.SaveChanges();
                        TempData["EmailVerifySuccess"] = "Email verified successfully";
                        return View();
                    }
                    else
                    {
                        TempData["Message"] = "Email already verified";
                        return View();
                    }
                }
            }
            else
            {
                TempData["Message"] = "Link is expired";
                return View();
            }
            return View();
        }


        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(AuthModel auth)
        {
            if (string.IsNullOrEmpty(auth.Email))
            {
                TempData["ResetPasswordErr"] = "Field Cannot be empty";
                return View();
            }

            var pattern = @"^[a-zA-Z0-9]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$";
            if (!Regex.IsMatch(auth.Email, pattern))
            {
                TempData["ResetPasswordErr"] = "Please enter the valid Email";
                return View();
            }

            var user = _db.Auths.FirstOrDefault(user => user.Email == auth.Email);
            if (user != null)
            {
                try
                {
                    var path = HttpContext.Request.Host;

                    auth.VerificationToken = Guid.NewGuid().ToString();

                    var subject = "Reset Password link";
                    byte[] emailbyte = Encoding.UTF8.GetBytes(auth.Email);
                    string encodeemail = Convert.ToBase64String(emailbyte);
                    /*byte[] tokenbyte = Encoding.UTF8.GetBytes(auth.VerificationToken);
                    string encodetoken = Convert.ToBase64String(tokenbyte);*/
                    byte[] datebyte = Encoding.UTF8.GetBytes(auth.CreatedAt.Add(TimeSpan.FromMinutes(10)).ToString());
                    string encodedate = Convert.ToBase64String(datebyte);
                    var htmlcontent = "<div>" +
                        "<p>Link is Valid for 10 minutes<p>" +
                        $"<a href='{path}/Authentication/ChangePassword?eid={encodeemail.Replace("=", "")}&ed={encodedate.Replace("=", "")}'>Click To Verify Email</a>" +
                        "</div>" +
                        $"https://{path}/Authentication/ChangePassword?eid={encodeemail}&ed={encodedate}";
                    var plaintext = "Email is vailed for 10 minutes";
                    _emailService.SendEmail(auth.FirstName + auth.LastName, auth.Email, subject, plaintext, htmlcontent);
                    TempData["ResetPasswordSuccess"] = "Email Sent Successfully";
                    return View("ResetPassword");
                }
                catch (Exception e)
                {
                    TempData["ResetPasswordErr"] = "Something went wrong pleas try again later";

                    return View();
                }
            }
            TempData["ResetPasswordErr"] = "Email sent to reset your password check your email";
            return View();
        }

        public IActionResult ChangePassword()
        {
            var query = Request.QueryString.ToUriComponent();
            NameValueCollection param = HttpUtility.ParseQueryString(query);
            //var eid = param["eid"];
            //var ed = param["ed"];
            byte[] emailbyte = Convert.FromBase64String(param["eid"]);
            string decodedemail = Encoding.UTF8.GetString(emailbyte);
            byte[] datebyte = Convert.FromBase64String(param["ed"]);
            string decodeddate = Encoding.UTF8.GetString(datebyte);

            if (Convert.ToDateTime(decodeddate) > DateTime.Now)
            {
                AuthModel auth = new AuthModel();
                var user = _db.Auths.FirstOrDefault(u => u.Email == decodedemail);
                if (user != null)
                {
                    auth.Email = user.Email;
                    auth.Id = user.Id;
                    return View(auth);
                }
            }
            else
            {
                TempData["linkExpired"] = "Link expired!";
                return View();
            }
            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(AuthModel auth)
        {

            var user = _db.Auths.FirstOrDefault(u => u.Email == auth.Email);
            if (user != null)
            {
                // Update the password in the database
                user.Password = auth.Password;
                _db.Auths.Update(user);
                _db.SaveChanges();
                TempData["ChangePasswordSuccess"] = "Changed Password Successfully!";
                return RedirectToAction("PasswordChangeSuccess");
            }
            else
            {
                TempData["ChangePasswordErr"] = "User not found!";
                return View();
            }
        }

        public IActionResult PasswordChangeSuccess()
        {
            return View();
        }


    }
}