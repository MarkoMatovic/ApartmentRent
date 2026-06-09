import React from 'react';
import { Container, Typography, Box, Divider, Link } from '@mui/material';

const Section: React.FC<{ title: string; children: React.ReactNode }> = ({ title, children }) => (
  <Box sx={{ mb: 4 }}>
    <Typography variant="h6" fontWeight="bold" gutterBottom>{title}</Typography>
    {children}
    <Divider sx={{ mt: 3 }} />
  </Box>
);

const P: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <Typography variant="body1" paragraph sx={{ lineHeight: 1.8 }}>{children}</Typography>
);

const Li: React.FC<{ children: React.ReactNode }> = ({ children }) => (
  <Typography component="li" variant="body1" sx={{ mb: 0.5 }}>{children}</Typography>
);

const PrivacyPolicyPage: React.FC = () => (
  <Container maxWidth="md" sx={{ py: 6 }}>
    <Typography variant="h4" fontWeight="bold" gutterBottom>Politika privatnosti</Typography>
    <Typography variant="body2" color="text.secondary" gutterBottom>
      Poslednje ažuriranje: jun 2026. | Primenjuje se od: dana objave
    </Typography>
    <Divider sx={{ mb: 4 }} />

    <Section title="1. Ko je rukovalac podataka?">
      <P>
        TuRentaj (u daljem tekstu: „Platforma", „mi", „nas") je oglasna platforma za pronalazak stanova i
        cimera na teritoriji Srbije. Platforma obrađuje lične podatke korisnika u skladu sa
        <strong> Zakonom o zaštiti podataka o ličnosti (ZZPL, Sl. glasnik RS 87/2018)</strong>,
        koji je usklađen sa EU Opštom uredbom o zaštiti podataka (GDPR).
      </P>
      <P>
        <strong>Kontakt rukavaoca:</strong><br />
        E-mail: <Link href="mailto:info@turentaj.com">info@turentaj.com</Link><br />
        Za zahteve u vezi zaštite podataka: <Link href="mailto:privacy@turentaj.com">privacy@turentaj.com</Link>
      </P>
    </Section>

    <Section title="2. Koje podatke prikupljamo?">
      <P>Prikupljamo sledeće kategorije ličnih podataka:</P>
      <Box component="ul" sx={{ pl: 3 }}>
        <Li><strong>Podaci o nalogu:</strong> ime, prezime, e-mail adresa, lozinka (hashirana BCrypt algoritmom), datum rođenja, broj telefona, profilna fotografija.</Li>
        <Li><strong>Podaci oglasa:</strong> adresa nekretnine, opis, fotografije (EXIF metapodaci se automatski uklanjaju pri upload-u), cena, kontakt telefon.</Li>
        <Li><strong>Podaci o plaćanju:</strong> broj narudžbenice, status transakcije. <strong>Ne čuvamo podatke kartice</strong> — platnu kartičnu obradu vrši Monri Payments d.o.o. (PCI DSS Level 1 sertifikat).</Li>
        <Li><strong>Komunikacijski podaci:</strong> poruke između korisnika, obaveštenja, e-mailovi platforme.</Li>
        <Li><strong>Tehnički podaci:</strong> IP adresa, vrsta pregledača, log zapisi (čuvaju se 14 dana), kolačići sesije.</Li>
        <Li><strong>Analitički podaci:</strong> pregledi oglasa, vreme provedeno na stranici, pretrage (samo za korisnike koji su kupili analitičku pretplatu).</Li>
      </Box>
    </Section>

    <Section title="3. Pravni osnov i svrha obrade">
      <Box component="ul" sx={{ pl: 3 }}>
        <Li><strong>Izvršenje ugovora (čl. 12(1)(b) ZZPL):</strong> registracija naloga, objavljivanje oglasa, slanje poruka, procesiranje plaćanja.</Li>
        <Li><strong>Legitimni interes (čl. 12(1)(f) ZZPL):</strong> bezbednost platforme, sprečavanje prevara, logovanje grešaka, unapređenje usluge.</Li>
        <Li><strong>Pristanak (čl. 12(1)(a) ZZPL):</strong> marketinške e-mail poruke (samo uz vašu izričitu saglasnost), kolačići za analitiku.</Li>
        <Li><strong>Zakonska obaveza (čl. 12(1)(c) ZZPL):</strong> čuvanje podataka o transakcijama u skladu sa poreskim propisima.</Li>
      </Box>
    </Section>

    <Section title="4. Sa kim delimo vaše podatke?">
      <Box component="ul" sx={{ pl: 3 }}>
        <Li><strong>Monri Payments d.o.o.</strong> — obrada platnih transakcija (podaci kartice nikad ne dospevaju do naše platforme).</Li>
        <Li><strong>Brevo (Sendinblue S.A.S.)</strong> — slanje transakcijskih e-mailova (dobrodošlica, verifikacija, obaveštenja).</Li>
        <Li><strong>Twilio Inc.</strong> — slanje SMS poruka (za verifikaciju i obaveštenja).</Li>
        <Li><strong>Microsoft Azure</strong> — hosting i infrastruktura (podaci se čuvaju u EU regionu).</Li>
      </Box>
      <P>Ne prodajemo, ne iznajmljujemo i ne razmenjujemo vaše podatke sa trećim stranama u marketinške svrhe.</P>
    </Section>

    <Section title="5. Koliko dugo čuvamo podatke?">
      <Box component="ul" sx={{ pl: 3 }}>
        <Li><strong>Podaci naloga:</strong> dok ne obrišete nalog + 30 dana (za slučaj oporavka).</Li>
        <Li><strong>Podaci oglasa:</strong> do brisanja oglasa ili naloga.</Li>
        <Li><strong>Log zapisi:</strong> 14 dana.</Li>
        <Li><strong>E-mail logovi:</strong> 90 dana (bez sadržaja poruka).</Li>
        <Li><strong>Podaci o transakcijama:</strong> 5 godina (zakonska obaveza, Zakon o PDV-u i računovodstvenim propisima RS).</Li>
        <Li><strong>Poruke između korisnika:</strong> čuvaju se dok korisnik ne obriše nalog ili ne povuče saglasnost za čuvanje istorije chata.</Li>
      </Box>
    </Section>

    <Section title="6. Vaša prava prema ZZPL">
      <P>Imate sledeća prava koja možete ostvariti slanjem zahteva na <Link href="mailto:privacy@turentaj.com">privacy@turentaj.com</Link>:</P>
      <Box component="ul" sx={{ pl: 3 }}>
        <Li><strong>Pravo na pristup (čl. 26 ZZPL):</strong> možete zatražiti kopiju svih vaših ličnih podataka koje obrađujemo.</Li>
        <Li><strong>Pravo na ispravku (čl. 29 ZZPL):</strong> ispravka netačnih ili nepotpunih podataka (dostupno i direktno u podešavanjima profila).</Li>
        <Li><strong>Pravo na brisanje (čl. 30 ZZPL):</strong> brisanje naloga i svih podataka (dostupno u podešavanjima profila).</Li>
        <Li><strong>Pravo na prenosivost (čl. 36 ZZPL):</strong> preuzimanje vaših podataka u mašinski čitljivom formatu (funkcija „Izvezi podatke" u profilu).</Li>
        <Li><strong>Pravo na prigovor (čl. 37 ZZPL):</strong> prigovor na obradu zasnovanu na legitimnom interesu.</Li>
        <Li><strong>Pravo na ograničenje obrade (čl. 31 ZZPL):</strong> privremeno zaustavljanje obrade podataka dok se rešava prigovor.</Li>
      </Box>
      <P>
        Na zahtev odgovaramo u roku od <strong>30 dana</strong>. Ako smatrate da su vaša prava povređena,
        možete podneti pritužbu <strong>Povereniku za informacije od javnog značaja i zaštitu podataka o ličnosti
        RS</strong> (poverenik.rs).
      </P>
    </Section>

    <Section title="7. Kolačići (Cookies)">
      <P>
        Koristimo isključivo <strong>funkcionalne kolačiće</strong> neophodne za rad platforme (autentifikaciona sesija).
        Ne koristimo kolačiće za praćenje ili ciljano oglašavanje trećih strana.
        Detaljne informacije pogledajte u našoj{' '}
        <Link href="/politika-kolacica">Politici kolačića</Link>.
      </P>
    </Section>

    <Section title="8. Bezbednost podataka">
      <P>
        Primenjujemo sledeće tehničke i organizacione mere zaštite: enkripcija lozinki (BCrypt), HTTPS/TLS
        za sve komunikacije, JWT tokeni sa kratkim rokom važnosti (15 min), httpOnly kolačići za refresh
        tokene, ograničenje broja pokušaja prijave (lockout), kontrola pristupa zasnovana na ulogama (RBAC),
        audit log svih promena podataka.
      </P>
    </Section>

    <Section title="9. Izmene politike privatnosti">
      <P>
        U slučaju značajnih izmena, obavestićemo vas e-mailom najmanje 14 dana pre stupanja izmena na snagu.
        Datum poslednjeg ažuriranja uvek je vidljiv na vrhu ove stranice.
      </P>
    </Section>
  </Container>
);

export default PrivacyPolicyPage;
