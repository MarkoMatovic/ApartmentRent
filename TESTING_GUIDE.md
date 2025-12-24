# Testing Guide - Communication System & Profile Update

## Serveri su pokrenuti ✅

- **Backend**: https://localhost:7092 (PID: 16524)
- **Frontend**: http://localhost:5173 (PID: 16888)

## Implementirane funkcionalnosti

### ✅ Sekcija 1: CRUD Operacije
- UpdateApartment endpoint
- GetUserProfile endpoint
- UpdateUserProfile endpoint
- DeleteReview endpoint
- DeleteFavorite endpoint
- GetUserFavorites endpoint
- DeleteNotification endpoint
- MarkAllAsRead endpoint

### ✅ Sekcija 2: Real-time Chat (SignalR)
**Backend:**
- `ChatHub` - SignalR hub za real-time messaging
- `MessagesController` - REST API endpointi
- `MessageService` - business logika
- Podržava group-based messaging, read receipts, typing indicators

**Frontend:**
- `ChatPage.tsx` - kompletna chat UI
- `useChatSignalR` hook - SignalR konekcija
- `messagesApi.ts` - API client

**Kako testirati:**
1. Otvori http://localhost:5173 i uloguj se
2. Navigiraj na Chat stranicu
3. Odaberi konverzaciju ili kreiraj novu poruku
4. Testirati možeš sa drugim korisnikom (otvori drugi browser ili incognito mode)
5. Poruke bi trebale stizati real-time bez refresh-a

### ✅ Sekcija 3: Email Notifikacije (SendGrid)
**Backend:**
- `EmailService` - SendGrid integracija
- Email templates (6 HTML fajlova):
  - WelcomeEmail.html
  - NewApplicationEmail.html
  - ApplicationStatusEmail.html
  - NewMessageEmail.html
  - AppointmentConfirmationEmail.html
  - SavedSearchAlertEmail.html
- Automatske pozadinske notifikacije:
  - Welcome email nakon registracije
  - Email notifikacija nakon nove poruke
- `EmailLogs` tabela za tracking

**Kako testirati:**
1. **VAŽNO**: Postavi SendGrid API key u `LandlordApp/appsettings.json` (linija 25)
2. Registruj novog korisnika → Proveri inbox za welcome email
3. Pošalji chat poruku → Primaoc treba da dobije email notifikaciju
4. Proveri `EmailLogs` tabelu u bazi za delivery status

**Napomena**: Bez SendGrid API key-a, email-ovi neće biti poslati, ali aplikacija će nastaviti da radi normalno (fire-and-forget pattern).

### ✅ Profile Update sa Image Upload i Deactivation
**Backend:**
- UpdateUserProfile endpoint (podržava partial updates)
- DeactivateUser endpoint
- Base64 image storage u bazi

**Frontend:**
- `ProfilePage.tsx` - potpuno prepisan (383 linije)
- Edit mode sa svim user poljima
- Image upload sa preview (max 5MB)
- Camera icon overlay na avatar-u
- Deactivate account dialog
- Roommate status toggle
- Success/Error alerts

**Kako testirati:**
1. Navigiraj na Profile Page
2. Klikni "Edit Profile"
3. Testirati:
   - Promeni ime/prezime/email/phone
   - Upload sliku (klikni camera icon)
   - Sačuvaj promene
   - Testirati Roommate toggle
   - Testirati Deactivate Account (oprezno - mora relogin)

## Testiranje REST API-ja (Postman/Swagger)

### Chat Endpoints
```
GET    /api/v1/messages/conversation?userId1=1&userId2=2&page=1&pageSize=50
GET    /api/v1/messages/user/{userId}
POST   /api/v1/messages/send
PUT    /api/v1/messages/mark-read/{messageId}
GET    /api/v1/messages/unread-count/{userId}
```

### User Profile Endpoints
```
GET    /api/v1/auth/profile/{userId}
PUT    /api/v1/auth/update-profile/{userId}
POST   /api/v1/auth/deactivate/{userGuid}
PUT    /api/v1/auth/update-roommate-status/{userGuid}
```

## SignalR Hub Endpoints

**ChatHub** - `/chatHub`

Metode:
- `JoinChatRoom(int userId)` - Pridruži se grupi
- `SendMessage(int senderId, int receiverId, string messageText)` - Pošalji poruku
- `MarkMessageAsRead(int messageId)` - Označi kao pročitano
- `UserTyping(int userId, int receiverId)` - Typing indicator

Events (server → client):
- `ReceiveMessage(MessageDto)` - Nova poruka primljena
- `MessageSent(MessageDto)` - Potvrda slanja
- `MessageRead({ messageId })` - Poruka pročitana
- `UserTyping({ userId })` - User kuca

## Database Changes

### Nove tabele:
- `EmailLogs` (Communication schema) - Email tracking

### Ažurirani modeli:
- `User` - Dodato `ProfilePicture` polje (base64)
- `Message` - Koristi se postojeći model iz Communication schema

## Poznati Issues / Warnings

1. **91 Nullability warnings** - Ne blokiraju build, ali bi trebali biti fiksirani u budućnosti
2. **SendGrid API key** - Mora se podesiti za email funkcionalnost
3. **Port conflict riješen** - Uklonjena Kestrel sekcija iz appsettings.json

## Kako stopirati servere

Backend:
```bash
Stop-Process -Id 16524
```

Frontend:
```bash
Stop-Process -Id 16888
```

Ili iz bash_background tool-a:
```
operation=stop, session_id=16524 (backend)
operation=stop, session_id=16888 (frontend)
```

## Šta dalje?

Prema planu, sledeće sekcije su:
- **Sekcija 4**: Apartment Applications (apliciranje tenant-a)
- **Sekcija 5**: Appointment Scheduling (zakazivanje razgledanja)
- **Sekcija 6**: Document Management (upload dokumenata)
- **Sekcija 7**: User Verification (email/phone/ID verification)
- **Sekcija 8**: Analytics & Statistics
- **Sekcija 9**: Saved Search Alerts (background jobs)

**Ništa nije komitovano** - čeka se testiranje i potvrda pre commit-a.

## Quick Test Checklist

- [ ] Backend se pokreće na portu 7092 bez grešaka
- [ ] Frontend se pokreće na portu 5173
- [ ] Login radi
- [ ] Chat stranica se učitava
- [ ] Mogu poslati poruku (REST fallback)
- [ ] SignalR konekcija uspešna (proveri browser console)
- [ ] Profile stranica se učitava
- [ ] Mogu edit-ovati profil
- [ ] Image upload radi
- [ ] Roommate toggle radi
- [ ] SendGrid API key postavljen (optional)
- [ ] Welcome email primljen nakon registracije (ako SendGrid je setup)

---

**Napomena**: Ako imaš bilo kakve probleme, proveri:
1. Browser console za JavaScript greške
2. Backend terminal output za exception-e
3. Network tab za failed API calls
4. Database za persisted data
