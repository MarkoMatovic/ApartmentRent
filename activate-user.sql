-- Aktiviraj korisnika sa email-om marko.matovic.6992@gmail.com
UPDATE Users 
SET IsActive = 1 
WHERE Email = 'marko.matovic.6992@gmail.com';

-- Proveri status
SELECT UserId, Email, FirstName, LastName, IsActive, IsLookingForRoommate 
FROM Users 
WHERE Email = 'marko.matovic.6992@gmail.com';
