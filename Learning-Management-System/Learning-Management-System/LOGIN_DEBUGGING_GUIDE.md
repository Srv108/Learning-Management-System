# 🔍 Login Debugging Guide - Step by Step

## The Issue You're Experiencing

When you click "Sign In":
1. ✅ Page sends request
2. ✅ Server processes (you see console data appear)
3. ❌ Page suddenly refreshes/reloads
4. ❌ Doesn't redirect to home page

## How to Debug This - BROWSER STEPS

### Step 1: Open Developer Tools
1. Go to http://localhost:5171/AuthMvc/Login
2. Press **F12** to open Developer Tools
3. Click the **Network** tab
4. Click the **Console** tab and keep it open too

### Step 2: BEFORE You Click Sign In
1. In the Network tab, right-click and select "Clear Network Log"
2. Make sure **Preserve Log** is **CHECKED** ✓

### Step 3: Try to Login
1. Enter email: `student1@lms.com`
2. Enter password: `Password@123`
3. Click **Sign In** button
4. **DO NOT CLICK ANYTHING** - just watch

### Step 4: Watch What Happens in Network Tab

You should see these requests appear in order:

**Request 1: POST /AuthMvc/Login**
- Method: POST
- Status: Should be 302 (REDIRECT) or 200
- Headers: Look for `Set-Cookie` header

**Request 2: GET / (if redirect followed)**
- Method: GET  
- Status: Should be 200
- This means redirect worked!

---

## What Each Status Code Means

```
✅ 200 = Success (page loaded)
✅ 302 = Redirect (server sending redirect instruction)
✅ 301 = Permanent Redirect

❌ 404 = Not Found
❌ 500 = Server Error
❌ 401 = Unauthorized
```

---

## The Cookie Trail

### In Request 1 (POST /AuthMvc/Login) Response Headers:
Look for: `Set-Cookie: .AspNetCore.Identity.Application=...`

If you see this ✅, the server IS setting the auth cookie!

### In Request 2 (GET /) Request Headers:
Look for: `Cookie: .AspNetCore.Identity.Application=...`

If you see this ✅, the browser IS sending the cookie back!

---

## Troubleshooting Checklist

### Check 1: Are cookies being set?
1. Network tab → Request 1 → Response Headers
2. Look for: `Set-Cookie`
3. If missing ❌ → Server not setting cookie
4. If present ✅ → Continue to Check 2

### Check 2: Is redirect happening?
1. Network tab → look at both requests
2. Do you see:
   - Request 1 → Status 302? ✓
   - Request 2 → GET /?  ✓
3. If NO ❌ → Form submission is reloading page instead of redirecting
4. If YES ✅ → Continue to Check 3

### Check 3: Is final page authenticated?
1. After all requests, check Console
2. Type: `document.cookie`
3. Should show: `.AspNetCore.Identity.Application=...`
4. If empty ❌ → Browser not keeping cookie
5. If has value ✅ → Cookie is stored

### Check 4: Is page content correct?
1. After login, on the home page
2. Look for text like: "Welcome", "Dashboard", "System Status"
3. If present ✅ → You're logged in!
4. If not ✅ → You're on login page still (redirect didn't work)

---

## Common Issues & Solutions

### Issue: See POST but no GET
**Problem:** Form posts but doesn't redirect
**Solution:** 
- Check POST Request → Response Headers for `Set-Cookie`
- If missing → Server error in login
- Check the Response body for error message

### Issue: See both POST and GET, but page still looks like login
**Problem:** Redirect is happening but authentication isn't persisting
**Solution:**
- In Console, type: `document.cookie`
- If empty → Cookie not being set
- If has value → Cookie is there, but page needs refresh

### Issue: Console shows error message
**Problem:** JavaScript error preventing form submission
**Solution:**
- In Inspector tab (next to Console), look for red errors
- Report the error message

### Issue: Page keeps redirecting back to login
**Problem:** Authentication works but then immediately logs out
**Solution:**
- Console → type: `document.cookie`
- Check if cookie is still there after redirect
- May need to check if authentication middleware is working

---

## What to Tell Me

When you report the issue, tell me:

1. **Network Tab - First Request (POST /AuthMvc/Login):**
   - What's the Status Code? ___
   - In Response Headers, do you see `Set-Cookie: .AspNetCore...`? ___

2. **Network Tab - Second Request (if it exists):**
   - Do you see a GET / request? ___
   - What's its Status Code? ___

3. **After All Requests:**
   - Console → `document.cookie` shows: ___
   - Does page look like login or home? ___

4. **Final Page Content:**
   - Do you see "Welcome" or "Dashboard"? ___
   - Do you see error message? ___

---

## Quick Console Commands

Copy/paste into Console (F12 → Console tab):

```javascript
// Check if cookie is set
console.log("Cookies:", document.cookie);

// Check if user is authenticated (from page's perspective)
console.log("Page data:", document.body.innerHTML.substring(0, 500));

// Check local storage
console.log("Local Storage:", localStorage);

// Check session storage  
console.log("Session Storage:", sessionStorage);
```

---

## The Exact Debug Flow

```
1. You click Sign In
   ↓
2. Browser: POST /AuthMvc/Login with email & password
   ↓
3. Server processes login (checks API, gets JWT, sets cookie)
   ↓
4. Server sends back: HTTP 302 + Set-Cookie header
   ↓
5. Browser: "Oh, 302 means redirect to /"
   ↓
6. Browser: Sends cookie with next request
   ↓
7. Browser: GET /
   ↓
8. Server: Checks if user has valid cookie
   ↓
9. Server: YES, send home page
   ↓
10. Browser displays home page
    ↓
✅ DONE - You're logged in!
```

---

## Server-Side Logs You Should See

When you login, the server console should show:

```
[LOGIN] Attempting login for: student1@lms.com
[LOGIN] Calling API: http://localhost:5171/api/auth/login
[LOGIN] API Response Status: OK
[LOGIN] Succeeded: True
[LOGIN] Token received: YES
[AUTH COOKIE] Setting authentication cookie for: student1@lms.com
[AUTH COOKIE] Calling SignInAsync
[AUTH COOKIE] SignInAsync completed
[LOGIN] Redirecting to Home
[HOME] Index called
[HOME] User.Identity.IsAuthenticated: True
[HOME] User is authenticated, rendering home view
```

---

## Next Step

**Please provide:**

1. Screenshot of Network tab showing both requests and their Status codes
2. Screenshot of Response Headers from the POST request
3. What does `document.cookie` show in Console?
4. Does the final page look like login or home page?

With this info, I can pinpoint exactly what's wrong!
