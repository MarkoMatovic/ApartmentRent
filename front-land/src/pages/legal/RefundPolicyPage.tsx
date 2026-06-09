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

const RefundPolicyPage: React.FC = () => (
  <Container maxWidth="md" sx={{ py: 6 }}>
    <Typography variant="h4" fontWeight="bold" gutterBottom>Politika povraćaja i otkazivanja</Typography>
    <Typography variant="body2" color="text.secondary" gutterBottom>
      Poslednje ažuriranje: jun 2026. | Primenjuje se na sve kupovine premium usluga TuRentaj platforme
    </Typography>
    <Divider sx={{ mb: 4 }} />

    <Alert severity="warning" sx={{ mb: 4 }}>
      Sve usluge TuRentaj platforme su <strong>digitalne usluge</strong> koje se isporučuju odmah po
      kupovini. U skladu sa Zakonom o zaštiti potrošača RS (čl. 30, st. 6), kupovinom ovih usluga
      korisnik se <strong>izričito odriče prava na odustajanje od ugovora u roku od 14 dana</strong>.
    </Alert>

    <Section title="1. Opšte pravilo — bez povraćaja za digitalne usluge">
      <P>
        Pošto sve naše premium usluge (analitika, tokeni, boost profila, isticanje oglasa, listing krediti,
        priority inbox) počinju da se isporučuju <strong>odmah</strong> po potvrdi plaćanja, na njih se
        primenjuje izuzetak od prava na odustajanje predviđen Zakonom o zaštiti potrošača RS.
      </P>
      <P>
        Korisnik pri svakoj kupovini potvrđuje checkbox: <em>„Razumijem da odricanjem od prava na
        odustajanje gubim pravo na povraćaj jer je digitalna usluga aktivirana odmah."</em>
      </P>
    </Section>

    <Section title="2. Izuzeci — kada imate pravo na povraćaj">
      <P>TuRentaj će izvršiti povraćaj u sledećim slučajevima:</P>
      <Box component="ul" sx={{ pl: 3 }}>
        <Li><strong>Tehnička greška naše platforme</strong> — usluga je naplaćena ali nije aktivirana (nije isporučena).</Li>
        <Li><strong>Duplo naplaćivanje</strong> — ista kupovina naplaćena više puta usled sistemske greške.</Li>
        <Li><strong>Platna greška procesora</strong> — plaćanje neuspešno ali iznos skinut s kartice (kontaktirajte Monri i nas istovremeno).</Li>
      </Box>
      <P>
        Zahtev za povraćaj usled gore navedenih razloga podnesite na{' '}
        <Link href="mailto:info@turentaj.com">info@turentaj.com</Link> u roku od <strong>7 dana</strong>{' '}
        od datuma transakcije, s priloženim brojem narudžbenice i opisom problema.
      </P>
    </Section>

    <Section title="3. Specifičnosti po tipu usluge">
      <Box component="ul" sx={{ pl: 3 }}>
        <Li>
          <strong>Analitika (mesečna/godišnja):</strong> Nakon aktivacije, usluga je dostupna za ugovoreni
          period. Korisnik može deaktivirati pretplatu u sekciji „Moje pretplate" — pristup prestaje odmah,
          bez srazmjernog povraćaja za preostali period. Nema automatskog obnavljanja.
        </Li>
        <Li>
          <strong>Tokeni:</strong> Nepovratni nakon kupovine. Iskorišćeni tokeni se ne nadoknađuju.
          Neiskorišćeni tokeni ne ističu i ostaju na nalogu do upotrebe.
        </Li>
        <Li>
          <strong>Isticanje oglasa (Featured):</strong> Aktivira se odmah. Bez povraćaja za preostale
          dane ako korisnik ukloni oglas pre isteka perioda.
        </Li>
        <Li>
          <strong>Listing krediti:</strong> Neiskorišćeni krediti ostaju na nalogu. Bez povraćaja za
          neiskorišćene kredite.
        </Li>
        <Li>
          <strong>Boost profila / Priority inbox:</strong> Aktivira se odmah. Bez povraćaja za preostale
          dane perioda.
        </Li>
      </Box>
    </Section>

    <Section title="4. Procedura povraćaja">
      <P>Ako imate pravo na povraćaj (v. tačku 2):</P>
      <Box component="ul" sx={{ pl: 3 }}>
        <Li>Pošaljite zahtev na <Link href="mailto:info@turentaj.com">info@turentaj.com</Link> s: brojem narudžbenice, datumom transakcije, opisom problema.</Li>
        <Li>Odgovaramo u roku od <strong>5 radnih dana</strong>.</Li>
        <Li>Povraćaj se vrši na istu platnu karticu u roku od <strong>7–14 radnih dana</strong>, zavisno od vaše banke.</Li>
        <Li>Parcijalni povraćaj nije moguć za iskorišćene delove usluge.</Li>
      </Box>
    </Section>

    <Section title="5. Prigovor i alternativno rešavanje sporova">
      <P>
        Ako niste zadovoljni našim odgovorom, možete podneti prigovor Ministarstvu unutrašnje i spoljne
        trgovine RS (zastitapotrosaca.gov.rs) ili nadležnom sudu u Beogradu.
      </P>
    </Section>

    <Box sx={{ mt: 4 }}>
      <Typography variant="body2" color="text.secondary">
        Kontakt: <Link href="mailto:info@turentaj.com">info@turentaj.com</Link> |{' '}
        <Link href="/uslovi-koriscenja">Uslovi korišćenja</Link> |{' '}
        <Link href="/support">Podrška</Link>
      </Typography>
    </Box>
  </Container>
);

export default RefundPolicyPage;
