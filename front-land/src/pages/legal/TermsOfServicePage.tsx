import React from 'react';
import { Container, Typography, Box, Divider, Link, Alert } from '@mui/material';

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

const TermsOfServicePage: React.FC = () => (
  <Container maxWidth="md" sx={{ py: 6 }}>
    <Typography variant="h4" fontWeight="bold" gutterBottom>Uslovi korišćenja</Typography>
    <Typography variant="body2" color="text.secondary" gutterBottom>
      Poslednje ažuriranje: jun 2026. | Važe za sve korisnike platforme TuRentaj
    </Typography>
    <Divider sx={{ mb: 4 }} />

    <Alert severity="info" sx={{ mb: 4 }}>
      <strong>Važno:</strong> TuRentaj je isključivo <strong>oglasna platforma</strong> (oglasnik) i
      <strong> ne vrši posredovanje u prometu i zakupu nepokretnosti</strong> u smislu Zakona o posredovanju
      u prometu i zakupu nepokretnosti (Sl. glasnik RS 95/2013). Platforma nije strana ni u jednom ugovoru
      između stanodavca i stanara.
    </Alert>

    <Section title="1. Prihvatanje uslova">
      <P>
        Korišćenjem platforme TuRentaj (turentaj.com) potvrđujete da ste pročitali, razumeli i prihvatili
        ove Uslove korišćenja. Ako se ne slažete sa uslovima, molimo vas da ne koristite platformu.
        Korišćenje platforme od strane maloletnih lica (mlađih od 18 godina) nije dozvoljeno.
      </P>
    </Section>

    <Section title="2. Šta je TuRentaj?">
      <P>
        TuRentaj je <strong>online oglasnik</strong> koji omogućava fizičkim licima i agencijama da
        objavljuju oglase za iznajmljivanje nepokretnosti i pronalazak cimera, kao i tražiteljima stanova
        da pregledaju te oglase i stupaju u kontakt sa stanodavcima.
      </P>
      <P>
        <strong>TuRentaj NIJE i NE OBAVLJA:</strong>
      </P>
      <Box component="ul" sx={{ pl: 3 }}>
        <Li>Posredovanje u prometu ili zakupu nepokretnosti.</Li>
        <Li>Provjeru tačnosti, zakonitosti ili autentičnosti objavljenih oglasa.</Li>
        <Li>Zastupanje ni jedne strane u pregovorima ili sklapanju ugovora o zakupu.</Li>
        <Li>Kontrolu platežne sposobnosti korisnika niti garaniju za ikakve transakcije.</Li>
      </Box>
      <P>
        Stanodavci i stanari direktno pregovaraju i sklapaju ugovor o zakupu bez učešća platforme.
      </P>
    </Section>

    <Section title="3. Registracija i nalog">
      <P>Za korišćenje svih funkcionalnosti potrebna je registracija. Obavezujete se da ćete:</P>
      <Box component="ul" sx={{ pl: 3 }}>
        <Li>Navesti tačne i potpune podatke pri registraciji.</Li>
        <Li>Čuvati sigurnost lozinke i prijaviti svaku neovlašćenu upotrebu naloga.</Li>
        <Li>Imati samo jedan nalog po osobi.</Li>
        <Li>Biti stariji od 18 godina.</Li>
      </Box>
      <P>
        TuRentaj zadržava pravo suspenzije ili trajnog brisanja naloga koji krši ove uslove, bez prethodne
        najave.
      </P>
    </Section>

    <Section title="4. Pravila objavljivanja oglasa">
      <P>Korisnik koji objavljuje oglas garantuje da:</P>
      <Box component="ul" sx={{ pl: 3 }}>
        <Li>Je vlasnik ili zakoniti zastupnik nepokretnosti koja se oglašava, ili ima ovlaštenje vlasnika.</Li>
        <Li>Su svi podaci u oglasu (cena, adresa, karakteristike) tačni i ažurni.</Li>
        <Li>Fotografije prikazuju opisanu nepokretnost i da ima pravo na njihovo objavljivanje.</Li>
        <Li>Oglas ne krši nijedan propis RS, niti prava trećih lica.</Li>
      </Box>
      <P><strong>Zabranjeno je objavljivanje:</strong></P>
      <Box component="ul" sx={{ pl: 3 }}>
        <Li>Lažnih, obmanjujućih ili nepostojećih oglasa.</Li>
        <Li>Oglasa koji diskriminišu po osnovu nacionalnosti, rase, pola, vere ili invaliditeta (Zakon o zabrani diskriminacije RS).</Li>
        <Li>Nepokretnosti bez pravnog osnova za zakup.</Li>
        <Li>Duplikata istog oglasa.</Li>
        <Li>Kontakt podataka u fotografijama (telefonski brojevi, e-mail adrese na slikama).</Li>
      </Box>
      <P>
        TuRentaj može ukloniti oglas koji krši ova pravila bez nadoknade korisnika za eventualno plaćene
        usluge vezane uz taj oglas.
      </P>
    </Section>

    <Section title="5. Premium usluge i plaćanje">
      <P>
        TuRentaj nudi opcione plaćene usluge (analitika, isticanje oglasa, listing krediti, boost profila,
        priority inbox, tokeni). Plaćanje se vrši putem Monri Payments platnog prolaza.
      </P>
      <Box component="ul" sx={{ pl: 3 }}>
        <Li>Sve cene su prikazane u eurima (EUR), bez PDV-a (ako je TuRentaj PDV obveznik, PDV se prikazuje posebno).</Li>
        <Li>Sva plaćanja su jednokratna (nema automatskog obnavljanja pretplate).</Li>
        <Li>Usluge digitalne prirode aktiviraju se odmah po uspešnoj uplati.</Li>
        <Li>Kupovinom digitalne usluge izričito se odričete prava na odustajanje od ugovora (v. tačku 7).</Li>
      </Box>
    </Section>

    <Section title="6. Odgovornost platforme">
      <P>TuRentaj nije odgovoran za:</P>
      <Box component="ul" sx={{ pl: 3 }}>
        <Li>Tačnost, zakonitost ili kvalitet objavljenih oglasa i nepokretnosti.</Li>
        <Li>Direktne ili indirektne štete nastale iz interakcije između stanodavaca i stanara.</Li>
        <Li>Privremenu nedostupnost platforme zbog tehničkih radova ili okolnosti van naše kontrole (viša sila).</Li>
        <Li>Gubitak podataka nastao van naše kontrole (npr. hakerski napad treće strane uprkos primenjenoj zaštiti).</Li>
      </Box>
      <P>
        Naša ukupna odgovornost prema jednom korisniku u svakom slučaju ne može preći iznos koji je taj
        korisnik platio platformi u poslednjih 12 meseci.
      </P>
    </Section>

    <Section title="7. Pravo na odustajanje od ugovora">
      <P>
        U skladu sa Zakonom o zaštiti potrošača RS (čl. 30, st. 6 — digitalni sadržaj i usluge):
        kupovinom premium usluge i izričitim potvrđivanjem pri kupovini, <strong>odričete se prava na
        odustajanje od ugovora u roku od 14 dana</strong>, jer se digitalna usluga isporučuje odmah
        po kupovini.
      </P>
      <P>
        Izuzetak: ako usluga nije aktivirana zbog tehničke greške na strani TuRentaj-a, imate pravo
        na povraćaj. Kontaktirajte nas na{' '}
        <Link href="mailto:info@turentaj.com">info@turentaj.com</Link>.
      </P>
      <P>
        Za detalje o povraćajima pogledajte našu{' '}
        <Link href="/politika-povracaja">Politiku povraćaja</Link>.
      </P>
    </Section>

    <Section title="8. Intelektualna svojina">
      <P>
        Sav sadržaj platforme (dizajn, softver, logotip, tekstovi) je vlasništvo TuRentaj-a i zaštićen
        autorskim pravom. Korisnici zadržavaju pravo na sopstveni sadržaj (fotografije, opisi oglasa) i
        daju TuRentaj-u neekskluzivnu licencu za prikaz tog sadržaja na platformi.
      </P>
    </Section>

    <Section title="9. Merodavno pravo i nadležnost">
      <P>
        Na ove Uslove korišćenja primenjuje se pravo <strong>Republike Srbije</strong>.
        Za sve sporove nadležan je sud u Beogradu, uz prethodnu obaveznu proceduru vansudskog rešavanja
        spora (kontakt: <Link href="mailto:info@turentaj.com">info@turentaj.com</Link>).
      </P>
    </Section>

    <Section title="10. Izmene uslova">
      <P>
        TuRentaj može izmeniti ove Uslove korišćenja. Korisnici će biti obavešteni e-mailom najmanje
        14 dana pre stupanja izmena na snagu. Nastavak korišćenja platforme nakon tog roka podrazumeva
        prihvatanje izmenjenih uslova.
      </P>
    </Section>

    <Box sx={{ mt: 4 }}>
      <Typography variant="body2" color="text.secondary">
        Za sva pitanja: <Link href="mailto:info@turentaj.com">info@turentaj.com</Link> |{' '}
        <Link href="/support">Podrška</Link>
      </Typography>
    </Box>
  </Container>
);

export default TermsOfServicePage;
