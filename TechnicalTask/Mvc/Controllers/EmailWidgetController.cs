using SitefinityWebApp.Mvc.Models;
using System.Web.Mvc;
using Telerik.Sitefinity.Mvc;
using Telerik.Sitefinity.Security;
using Telerik.Sitefinity.Security.Model;
using Telerik.Sitefinity.Security.Claims;
using System.Web.Security;
using System;
using Telerik.Sitefinity.Forums;
using Telerik.Sitefinity.Forums.Model;
using System.Text.RegularExpressions;

namespace SitefinityWebApp.Mvc.Controllers
{
    [ControllerToolboxItem(Name = "EmailWidget", Title = "Email Widget", SectionName = "Forum Post Widgets")]
    public class EmailWidgetController : Controller
    {
        private string StatusMessage { get; set; }      

        [HttpGet]
        public ActionResult Index()
        {
            return View("EmailWidget");
        }

        [HttpPost]
        public ActionResult CreateForum(EmailWidgetModel model)
        {

            if (!this.ModelState.IsValid)
            {
                return View("EmailWidget", model);
            }

            var userManager = UserManager.GetManager();
            var profileManager = UserProfileManager.GetManager();
            var identity = ClaimsManager.GetCurrentIdentity();            

            if (!this.User.Identity.IsAuthenticated)
            {
                this.StatusMessage = "Error: You do not have sufficient permission. Please log in.";
                return View("StatusMessage", (object)this.StatusMessage);
            }

            if (!userManager.EmailExists(model.Email))
            {
                MembershipCreateStatus isCreated = CreateUser(model.Email);

                if (isCreated != MembershipCreateStatus.Success)
                {
                    this.StatusMessage = "Error creating new user. Please use registration form.";
                    return View("StatusMessage", (object)this.StatusMessage);
                }

                this.StatusMessage = $"Successfully created user with user name: {model.Email}";

                return View("StatusMessage", (object)this.StatusMessage);
            }

            var authenticatedUserId = identity.UserId;
            var user = userManager.GetUser(authenticatedUserId);
            var authenticatedUserName = user.UserName;


            bool isUserIsAdministrator = IsUserInRole(authenticatedUserName);

            if (!isUserIsAdministrator)
            {
                this.StatusMessage = "Error: Sorry but you do not have enough permissions. Only Administrator can create forum thread.";
                return View("StatusMessage", (object)this.StatusMessage);
            }

            var inputUser = userManager.GetUser(model.Email);

            var userProfile = profileManager.GetUserProfile<SitefinityProfile>(inputUser);

            try
            {
                string forumTitle = CreateForumProvider(userProfile.FirstName, user.UserName);

                this.StatusMessage = $"Successfully created forum with title: {forumTitle}";
                return View("StatusMessage", (object)this.StatusMessage);
            }
            catch (Telerik.Sitefinity.Data.DuplicateUrlException ex)
            {
                this.StatusMessage = $"Error: {ex.Message}";
                return View("StatusMessage", (object)this.StatusMessage);
            }
        }

        [NonAction]
        private static MembershipCreateStatus CreateUser(string email)
        {
            UserManager userManager = UserManager.GetManager();
            UserProfileManager profileManager = UserProfileManager.GetManager();

            MembershipCreateStatus status;

            try
            {
                User user = userManager.CreateUser(email, email, email, email, true, null, out status);

                if (status == MembershipCreateStatus.Success)
                {
                    SitefinityProfile sfProfile = profileManager.CreateProfile(user, Guid.NewGuid(), typeof(SitefinityProfile)) as SitefinityProfile;

                    if (sfProfile != null)
                    {
                        sfProfile.FirstName = email;
                        sfProfile.LastName = email;
                    }

                    userManager.SaveChanges();
                    profileManager.RecompileItemUrls(sfProfile);
                    profileManager.SaveChanges();
                }

                return status;                
            }
            catch (System.UnauthorizedAccessException ex)
            {
                return MembershipCreateStatus.ProviderError;
            }        
        }

        [NonAction]
        private static bool IsUserInRole(string userName, string roleName = "Administrators")
        {
            bool isUserInRole = false;

            var userManager = UserManager.GetManager();
            var roleManager = RoleManager.GetManager(SecurityConstants.ApplicationRolesProviderName);

            bool userExists = userManager.UserExists(userName);
            bool roleExists = roleManager.RoleExists(roleName);

            if (userExists && roleExists)
            {
                var user = userManager.GetUser(userName);
                isUserInRole = roleManager.IsUserInRole(user.Id, roleName);
            }

            return isUserInRole;
        }

        [NonAction]
        private static string CreateForumProvider(string forumTitle, string forumDescription)
        {
            ForumsManager forumsManager = ForumsManager.GetManager();

            Forum forum = forumsManager.CreateForum();

            forum.Title = forumTitle + " forum";
            forum.Description = forumDescription + " description";
            forum.UrlName = Regex.Replace(forumTitle.ToLower(), @"[^\w\-\!\$\'\(\)\=\@\d_]+", "-");

            forumsManager.RecompileItemUrls<Forum>(forum);

            forumsManager.SaveChanges();
            return forum.Title;
        }
    }
}