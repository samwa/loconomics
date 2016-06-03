using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebMatrix.Data;
using WebMatrix.WebData;
using WebMatrix.Security;
using System.Web.WebPages;
using System.Web.Security;

/// <summary>
/// Utilities class about authentication and authorization,
/// covering account utilities (register, password,..) and access control.
/// TODO: To much of features that have better place here are located right now
/// on specific pages under Account/ folder or as wide used methods in generic LcHelpers
/// </summary>
public static class LcAuth
{
    public static bool ExistsEmail(string email)
    {
        using (var db = Database.Open("sqlloco"))
        {
            return db.QuerySingle("EXEC CheckUserEmail @0", email) != null;
        }
    }
    /// <summary>
    /// For HIPAA compliance (#974) and strong security, passwords must be checked against next regex. It requires:
    /// - Almost 8 characters
    /// - Non-alphabetic characters: ~!@#$%^*&;?.+_
    /// - Base 10 digits (0 through 9)
    /// - English uppercase characters (A through Z)
    /// - English lowercase characters (a through z)
    /// </summary>
    public static string ValidPasswordRegex = @"(?=.{8,})(?=.*?[^\w\s])(?=.*?[0-9])(?=.*?[A-Z]).*?[a-z].*";
    public static string InvalidPasswordErrorMessage = @"Your password must be at least 8 characters long, have at least: one lowercase letter, one uppercase letter, one symbol (~!@#$%^*&;?.+_), and one numeric digit.";
    public static string AccountLockedErrorMessage = @"Your account has been locked due to too many unsuccessful login attempts. Please try logging in again after 5 minutes or click Forget password";
    public class RegisteredUser
    {
        public string Email;
        public int UserID;
        public string ConfirmationToken;
        public bool IsProvider;
    }
    /// <summary>
    /// Register an user and returns relevant registration information about the new account,
    /// or raise and exception on error of type System.Web.Security.MembershipCreateUserException.
    /// IMPORTANT: For code that doesn't uses this but the CreateAccount directly,
    /// is required to validate the password against ValidPasswordRegex. 
    /// It's recommended to use form validation with that regex before even call this to avoid extra computation, checks,
    /// but this will check the regex too.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="firstname"></param>
    /// <param name="lastname"></param>
    /// <param name="password"></param>
    /// <param name="isProvider"></param>
    /// <returns></returns>
    public static RegisteredUser RegisterUser(
        string email,
        string firstname,
        string lastname,
        string password,
        bool isProvider,
        string marketingSource = null,
        int genderID = -1,
        string aboutMe = null,
        string phone = null,
        string signupDevice = null
    ) {
        // Check password validity.
        if (!System.Text.RegularExpressions.Regex.IsMatch(password, ValidPasswordRegex, System.Text.RegularExpressions.RegexOptions.ECMAScript))
        {
            throw new ConstraintException(InvalidPasswordErrorMessage);
        }

        using (var db = Database.Open("sqlloco"))
        {
            // IMPORTANT: The whole process must be complete or rollback, but since
            // a transaction cannot be done from the start because will collide
            // with operations done by WebSecurity calls, the first step must
            // be protected manually.
            string token = null;

            try
            {
                // Insert email into the profile table
                db.Execute("INSERT INTO UserProfile (Email) VALUES (@0)", email);

                // Create and associate a new entry in the membership database (is connected automatically
                // with the previous record created using the automatic UserID generated for it).
                token = WebSecurity.CreateAccount(email, password, true);
            }
            catch (Exception ex)
            {
                // Manual rollback previous operation:
                // If CreateAccount failed, nothing was persisted there so nothing require rollback,
                // only the UserProfile record
                db.Execute("DELETE FROM UserProfile WHERE Email like @0", email);

                // Relay exception
                throw ex;
            }

            // Create Loconomics Customer user
            int userid = WebSecurity.GetUserId(email);

            try
            {
                // Automatic transaction can be used now:
                db.Execute("BEGIN TRANSACTION");

                // TODO:CONFIRM: SQL executed inside a procedure is inside the transaction? Some errors on testing showed that maybe not, and that's a problem.
                db.Execute("exec CreateCustomer @0,@1,@2,@3,@4,@5,@6,@7",
                    userid, firstname, lastname,
                    LcData.GetCurrentLanguageID(), LcData.GetCurrentCountryID(),
                    genderID, aboutMe, phone
                );

                // If is provider, update profile with that info (being both customer and provider)
                // It assigns the first OnboardingStep 'welcome' for the new Onboarding Dashboard #454
                if (isProvider)
                {
                    BecomeProvider(userid, db);
                }

                // Partial email confirmation to allow user login but still show up email-confirmation-alert. Details:
                // IMPORTANT: 2012-07-17, issue #57; We decided use the email-confirmation-code only as a dashboard alert (id:15) instead of blocking the user
                // login, what means user MUST can login but too MUST have an email-confirmation-code; we do that reusing the confirmation code
                // created by asp.net starter-app as until now, but HACKING that system doing a minor change on database, in the 
                // asp.net webpages generated table called 'webpages_Membership': there are two fields to manage confirmation, a bit field (this
                // we will hack changing it to true:1 manually -before of time-) and the confirmationToken that we will mantain to allow user confirmation
                // from the welcome-email sent and to off the alert:15 (with custom code on the Account/Confirm page).
                db.Execute(@"
                    UPDATE webpages_Membership SET
                        IsConfirmed = 1
                    WHERE UserId = @0
                ", userid);

                // Log Marketing URL parameters
                if (marketingSource == null && System.Web.HttpContext.Current != null)
                    marketingSource = System.Web.HttpContext.Current.Request.Url.Query;
                if (marketingSource != null)
                    db.Execute("UPDATE users SET MarketingSource = @1 WHERE UserID = @0", userid, marketingSource);

                // Device
                if (!string.IsNullOrEmpty(signupDevice))
                    db.Execute("UPDATE users SET SignupDevice = @1 WHERE UserID = @0", userid, signupDevice);

                db.Execute("COMMIT TRANSACTION");

                // All done:
                return new RegisteredUser
                {
                    UserID = userid,
                    Email = email,
                    ConfirmationToken = token,
                    IsProvider = isProvider
                };
            }
            catch (Exception ex)
            {
                db.Execute("ROLLBACK TRANSACTION");

                // If profile creation failed, there was a rollback, now must ensure the userprofile record is removed too:
                db.Execute("DELETE FROM UserProfile WHERE Email like @0", email);

                throw ex;
            }
        }
    }
    public static void BecomeProvider(int userID, Database db = null)
    {
        var ownDb = db == null;
        if (ownDb)
        {
            db = Database.Open("sqlloco");
        }

        // Provider profiles must have a BookCode, so generate one
        // (but not replace if one exists)
        var bookCode = LcData.UserInfo.GenerateBookCode(userID);
        
        db.Execute(@"UPDATE Users SET 
            IsProvider = 1,
            OnboardingStep = 'welcome',
            BookCode = @1
            WHERE UserID = @0
        ", userID, bookCode);

        if (ownDb)
        {
            db.Dispose();
        }
    }
    public static void SendRegisterUserEmail(RegisteredUser user)
    {
        // Sent welcome email (if there is a confirmationUrl and token values, the email will contain it to perform the required confirmation)
        if (user.IsProvider)
            LcMessaging.SendWelcomeProvider(user.UserID, user.Email);
        else
            LcMessaging.SendWelcomeCustomer(user.UserID, user.Email);
    }

    public static void ConnectWithFacebookAccount(int userID, long facebookID)
    {
        using (var db = Database.Open("sqlloco")){

            // Create asociation between locouser and facebookuser
            db.Execute(string.Format("INSERT INTO {0} ({1}, {2}) VALUES (@0, @1)", "webpages_FacebookCredentials", "UserId", "FacebookId"), userID, facebookID);
                            
            // Add Facebook verification as confirmed
            db.Execute(@"EXEC SetUserVerification @0,@1,@2,@3", userID, 8, DateTime.Now, 1);
            // Test social media alert
            db.Execute("EXEC TestAlertSocialMediaVerification @0", userID);
        }
    }

    public static int? GetFacebookUserID(long facebookID)
    {
        using (var db = Database.Open("sqlloco"))
        {
            return db.QueryValue("SELECT UserId FROM webpages_FacebookCredentials WHERE FacebookId=@0", facebookID);
        }
    }

    /// <summary>
    /// Get basic user info given a Facebook User ID or null.
    /// </summary>
    /// <param name="facebookID"></param>
    public static RegisteredUser GetFacebookUser(long facebookID)
    {
        int? userId = GetFacebookUserID(facebookID);
        if (userId.HasValue)
        {
            var userData = LcRest.UserProfile.Get(userId.Value);
            // Check is valid (only edge cases will not be a valid record,
            // as incomplete manual deletion of user accounts that didn't remove
            // the Facebook connection).
            if (userData != null)
            {
                return new RegisteredUser
                {
                    Email = userData.email,
                    IsProvider = userData.isServiceProfessional,
                    UserID = userId.Value
                };
            }
            return null;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Returns true if account is locked.
    /// It uses the general rules: As requested at #974, for HIPAA compliance:
    /// - Lock account after 5 unsuccesfully attempts
    /// - Lock it for 5 minutes
    /// </summary>
    /// <returns></returns>
    private static bool IsAccountLockedOut(string email)
    {
        return WebSecurity.IsAccountLockedOut(email, 5, 5 * 60);
    }

    public static bool Login(string email, string password, bool persistCookie = false)
    {
        if (IsAccountLockedOut(email))
            throw new ConstraintException(AccountLockedErrorMessage);
        // Navigate back to the homepage and exit
        var result = WebSecurity.Login(email, password, persistCookie);

        LcData.UserInfo.RegisterLastLoginTime(0, email);

        // mark the user as logged in via a normal account,
        // as opposed to via an OAuth or OpenID provider.
        System.Web.HttpContext.Current.Session["OAuthLoggedIn"] = false;

        return result;
    }
    /// <summary>
    /// Check a user autologinkey to performs the automatic login if
    /// match.
    /// If success, the request continue being processing but with a
    /// new session and new authentication cookie being sent in the response.
    /// </summary>
    /// <param name="userid"></param>
    /// <param name="autologinkey"></param>
    public static void Autologin(string userid, string autologinkey)
    {
        if (IsAccountLockedOut(userid))
            throw new ConstraintException(AccountLockedErrorMessage);

        try
        {
            using (var db = Database.Open("sqlloco"))
            {
                var p = db.QueryValue(@"
                    SELECT  Password
                    FROM    webpages_Membership
                    WHERE   UserId=@0
                ", userid);
                // TODO For performance and security, save a processed autologinkey in database
                // and check against that rather than do this tasks every time; auto compute on
                // any password change.
                // Check if autologinkey and password (encrypted and then converted for url) match
                if (autologinkey == LcEncryptor.ConvertForURL(LcEncryptor.Encrypt(p)))
                {
                    // Autologin Success
                    // Get user email by userid
                    var userEmail = db.QueryValue(@"
                        SELECT  email
                        FROM    userprofile
                        WHERE   userid = @0
                    ", userid);
                    // Clear current session to avoid conflicts:
                    if (HttpContext.Current.Session != null)
                        HttpContext.Current.Session.Clear();
                    
                    // New authentication cookie: Logged!
                    System.Web.Security.FormsAuthentication.SetAuthCookie(userEmail, false);

                    LcData.UserInfo.RegisterLastLoginTime(userid.AsInt(), userEmail);
                }
            }
        }
        catch (Exception ex) { HttpContext.Current.Response.Write(ex.Message); }
    }
    /// <summary>
    /// Get the key that enable the user to autologged from url, to
    /// be used by email templates.
    /// </summary>
    /// <param name="userid"></param>
    /// <returns></returns>
    public static string GetAutologinKey(int userid)
    {
        try
        {
            using (var db = Database.Open("sqlloco"))
            {
                var p = db.QueryValue(@"
                    SELECT  Password
                    FROM    webpages_Membership
                    WHERE   UserId=@0
                ", userid);
                return LcEncryptor.ConvertForURL(LcEncryptor.Encrypt(p));
            }
        }
        catch { }
        return null;
    }
    /// <summary>
    /// Request an autologin using the values from the HttpRequest if
    /// is need.
    /// </summary>
    public static void RequestAutologin(HttpRequest Request)
    {
        // First, check standard 'Authorization' header
        var auth = Request.Headers["Authorization"];
        if (!String.IsNullOrEmpty(auth))
        {
            var m = System.Text.RegularExpressions.Regex.Match(auth, "^LC alu=([^,]+),alk=(.+)$");
            if (m.Success)
            {
                var alu = m.Groups[1].Value;
                var alk = m.Groups[2].Value;
                LcAuth.Autologin(alu, alk);
            }
        }
        else
        {
            // Using custom headers first, best for security using the REST API.
            var Q = Request.QueryString;
            var alk = N.DW(Request.Headers["alk"]) ?? Q["alk"];
            var alu = N.DW(Request.Headers["alu"]) ?? Q["alu"];

            // Autologin feature for anonymous sessions with autologin parameters on request
            if (!Request.IsAuthenticated
                && alk != null
                && alu != null)
            {
                // 'alk' url parameter stands for 'Auto Login Key'
                // 'alu' url parameter stands for 'Auto Login UserID'
                LcAuth.Autologin(alu, alk);
            }
        }
    }
    /// <summary>
    /// Get for the given userID the params and values for autologin in URL string format,
    /// ready to be appended to an URL
    /// (without &amp; or ? as prefix, but &amp; as separator and last character).
    /// To be used mainly by Email templates.
    /// </summary>
    /// <returns></returns>
    public static string GetAutologinUrlParams(int userID)
    {
        return String.Format("alu={0}&alk={1}&",
            userID,
            GetAutologinKey(userID));
    }

    public static string GetConfirmationToken(int userID)
    {
        using (var db = new LcDatabase())
        {
            return userID == -1 ? null :
                // coalesce used to avoid the value 'DbNull' to be returned, just 'empty' when there is no token,
                // is already confirmed
                db.QueryValue("SELECT coalesce(ConfirmationToken, '') FROM webpages_Membership WHERE UserID=@0", userID);
        }
    }

    public static LcRest.UserProfile ConfirmAccount(string confirmationCode)
    {
        using (var db = new LcDatabase())
        {
            var userID = (int?)db.QueryValue(@"
                SELECT UserId FROM webpages_Membership
                WHERE ConfirmationToken = @0
            ", confirmationCode);

            if (userID.HasValue)
            {
                // Check if the account requires to complete the sign-up:
                // - it happens for user whose record was created by a professional (added him as client)
                // - so, the user has an accountStatusID serviceProfessional's client
                // -> On that case, we cannot confirm the account yet, since we need from the client to
                // complete the sign-up, generating a password by itself. We just follow up returning the user
                // profile data that can be used to pre-populate the 'client activation' sign-up form.
                var user = LcRest.UserProfile.Get(userID.Value);
                if (user.accountStatusID != (int)LcEnum.AccountStatus.serviceProfessionalClient)
                {
                    // User can confirm it's account, proceed:
                    db.Execute(@"
                        UPDATE webpages_Membership
                        SET ConfirmationToken = null, IsConfirmed = 1
                        WHERE ConfirmationToken like @0 AND UserID = @1
                    ", confirmationCode, userID);
                    // In the lines above, we cannot use the aps.net WebSecurity standard logic:
                    // //WebSecurity.ConfirmAccount(confirmationToken)
                    // because the change of confirmation first-time optional step, alert at dashboard
                    // and (sometimes this business logic changes) required for second and following login attempts.
                    // Because of this a hack is done on provider-sign-up login over the IsConfirmed field, and this becomes the ConfirmAccount
                    // standard method unuseful (do nothing, really, because it checks IsConfirmed field previuosly and ever is true, doing nothing -we need set to null 
                    // ConfirmationToken to off the alert-). On success, ConfirmationToken is set to null and IsConfirmed to 1 (true), supporting both cases, when IsConfirmed is
                    // already true and when no.
                    db.Execute("EXEC TestAlertVerifyEmail @0", userID);

                    // IMPORTANT: Since 2012-09-27, issue #134, Auto-login is done on succesful confirmation;
                    // some code after next lines (comented as 'starndard logic' will not be executed, and some html, but preserved as documentation)
                    // Confirmation sucess, we need user name (email) to auto-login:
                    FormsAuthentication.SetAuthCookie(user.email, false);
                }
                return user;
            }
        }
        return null;
    }

    public static bool HasMembershipRecord(int userID)
    {
        using (var db = new LcDatabase())
        {
            return db.QueryValue("SELECT userid FROM webpages_Membership WHERE userid = @0", userID) != null;
        }
    }
}