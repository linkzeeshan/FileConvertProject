using FileConvertPro.Models;
using FileConvertPro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FileConvertPro.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAccountService _accountService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAccountService accountService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _accountService = accountService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    CreatedAt = DateTime.UtcNow,
                    SubscriptionTier = SubscriptionTier.Free
                };

                var result = await _accountService.CreateUserAsync(user, model.Password, "Customer");

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    TempData["SuccessMessage"] = "Registration successful! Welcome to FileConvertPro.";
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    // Update last login time
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        user.LastLogin = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);

                        // Check subscription validity for admin users
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            bool isSubscriptionValid = await _accountService.IsSubscriptionValidAsync(user.Id);
                            if (!isSubscriptionValid && user.SubscriptionTier != SubscriptionTier.Free)
                            {
                                TempData["WarningMessage"] = "Your subscription has expired. Some admin features may be limited.";
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
                if (result.IsLockedOut)
                {
                    TempData["ErrorMessage"] = "Account locked out. Please try again later.";
                    return View(model);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                SubscriptionTier = user.SubscriptionTier,
                SubscriptionEndDate = user.SubscriptionEndDate
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your profile has been updated successfully.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet]
        public IActionResult CreateAdmin()
        {
            return View();
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    CreatedAt = DateTime.UtcNow,
                    SubscriptionTier = SubscriptionTier.Enterprise,
                    SubscriptionEndDate = DateTime.UtcNow.AddYears(1)
                };

                var result = await _accountService.CreateUserAsync(user, model.Password, "Admin");

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Admin user created successfully.";
                    return RedirectToAction("Dashboard", "Admin");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet]
        public IActionResult CreateCustomer()
        {
            return View();
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomer(CreateCustomerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    CreatedAt = DateTime.UtcNow,
                    SubscriptionTier = model.SubscriptionTier,
                    SubscriptionEndDate = model.SubscriptionTier != SubscriptionTier.Free ? 
                        DateTime.UtcNow.AddMonths(model.SubscriptionMonths) : null
                };

                var result = await _accountService.CreateUserAsync(user, model.Password, "Customer");

                if (result.Succeeded)
                {
                    // If customer has a paid subscription, also add them to Admin role
                    if (model.SubscriptionTier != SubscriptionTier.Free)
                    {
                        await _accountService.AssignRoleAsync(user, "Admin");
                    }

                    TempData["SuccessMessage"] = "Customer created successfully.";
                    return RedirectToAction("Dashboard", "Admin");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet]
        public async Task<IActionResult> ManageSubscription(string id)
        {
            var user = await _accountService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ManageSubscriptionViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                CurrentSubscriptionTier = user.SubscriptionTier,
                CurrentSubscriptionEndDate = user.SubscriptionEndDate,
                NewSubscriptionTier = user.SubscriptionTier,
                SubscriptionMonths = 1
            };

            return View(model);
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageSubscription(ManageSubscriptionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            DateTime endDate = model.NewSubscriptionTier != SubscriptionTier.Free ?
                DateTime.UtcNow.AddMonths(model.SubscriptionMonths) : DateTime.MinValue;

            var result = await _accountService.UpdateSubscriptionAsync(
                model.UserId, model.NewSubscriptionTier, endDate);

            if (result)
            {
                TempData["SuccessMessage"] = "Subscription updated successfully.";
                return RedirectToAction("UserDetails", "Admin", new { id = model.UserId });
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Failed to update subscription.");
                return View(model);
            }
        }
    }
}
