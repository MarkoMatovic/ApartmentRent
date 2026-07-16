import React from 'react';
import { Container, Typography, Box, Divider, Link, Table, TableBody, TableCell, TableHead, TableRow, Paper } from '@mui/material';

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

const CookiePolicyPage: React.FC = () => (
  <Container maxWidth="md" sx={{ py: 6 }}>
    <Typography variant="h4" fontWeight="bold" gutterBottom>Politika kolačića (Cookie Policy)</Typography>
    <Typography variant="body2" color="text.secondary" gutterBottom>
      Poslednje ažuriranje: jun 2026.
    </Typography>
    <Divider sx={{ mb: 4 }} />

    <Section title="1. Šta su kolačići?">
      <P>
        Kolačić (cookie) je mala tekstualna datoteka koju vaš pretraživač čuva na vašem uređaju kada
        posetite web sajt. Kolačići pomažu sajtu da zapamti vaše preference i omogućavaju sigurnu
        autentifikaciju.
      </P>
    </Section>

    <Section title="2. Koje kolačiće koristimo?">
      <P>TuRentaj koristi <strong>isključivo funkcionalne kolačiće</strong> neophodne za rad platforme.</P>

      <Paper variant="outlined" sx={{ mt: 2 }}>
        <Table size="small">
          <TableHead>
            <TableRow sx={{ bgcolor: 'action.hover' }}>
              <TableCell><strong>Naziv</strong></TableCell>
              <TableCell><strong>Svrha</strong></TableCell>
              <TableCell><strong>Trajanje</strong></TableCell>
              <TableCell><strong>Vrsta</strong></TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            <TableRow>
              <TableCell>refreshToken</TableCell>
              <TableCell>Sigurna autentifikacija — čuva token za obnavljanje sesije. Postavlja se kao httpOnly, Secure, SameSite=Strict kolačić (nedostupan JavaScript-u).</TableCell>
              <TableCell>30 dana</TableCell>
              <TableCell>Funkcionalni (neophodan)</TableCell>
            </TableRow>
            <TableRow>
              <TableCell>cookieConsent</TableCell>
              <TableCell>Čuva vašu odluku o prihvatanju/odbijanju kolačića kako ne bismo ponavljali upit.</TableCell>
              <TableCell>1 godina</TableCell>
              <TableCell>Funkcionalni (neophodan)</TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </Paper>

      <P>
        <strong>Ne koristimo:</strong> kolačiće za praćenje (tracking), kolačiće trećih strana za
        ciljano oglašavanje, Google Analytics, Facebook Pixel ni sličnih alata za profilisanje korisnika.
      </P>
    </Section>

    <Section title="3. Access token — nije kolačić">
      <P>
        JWT access token (kratkotrajan, 15 minuta) čuvamo isključivo <strong>u memoriji pregledača</strong>
        (JavaScript varijabla), a ne u kolačiću niti localStorage-u. Ovo je dizajnerska odluka kojom
        smanjujemo rizik od XSS napada.
      </P>
    </Section>

    <Section title="4. Upravljanje kolačićima">
      <P>
        Možete onemogućiti ili obrisati kolačiće u podešavanjima vašeg pregledača:
      </P>
      <Box component="ul" sx={{ pl: 3 }}>
        <Typography component="li" variant="body1" sx={{ mb: 0.5 }}>
          <strong>Chrome:</strong> Podešavanja → Privatnost i bezbednost → Kolačići
        </Typography>
        <Typography component="li" variant="body1" sx={{ mb: 0.5 }}>
          <strong>Firefox:</strong> Podešavanja → Privatnost i zaštita → Kolačići i podaci sajtova
        </Typography>
        <Typography component="li" variant="body1" sx={{ mb: 0.5 }}>
          <strong>Safari:</strong> Podešavanja → Privatnost → Upravljanje podacima sajtova
        </Typography>
        <Typography component="li" variant="body1" sx={{ mb: 0.5 }}>
          <strong>Edge:</strong> Podešavanja → Kolačići i dozvole sajtova
        </Typography>
      </Box>
      <P>
        <strong>Napomena:</strong> onemogućavanje kolačića refreshToken onemogućiće autentifikaciju
        i nećete moći da ostanete prijavljeni između sesija pregledača.
      </P>
    </Section>

    <Section title="5. Zakonski osnov">
      <P>
        Funkcionalni kolačići neophodni za rad platforme postavljaju se na osnovu <strong>legitimnog
        interesa</strong> i bez potrebe za posebnom saglasnošću, u skladu sa ZZPL i
        direktivom ePrivacy 2002/58/EC (primenjenom u srpskom zakonodavstvu).
      </P>
    </Section>

    <Box sx={{ mt: 4 }}>
      <Typography variant="body2" color="text.secondary">
        Pitanja: <Link href="mailto:privacy@turentaj.com">privacy@turentaj.com</Link> |{' '}
        <Link href="/politika-privatnosti">Politika privatnosti</Link>
      </Typography>
    </Box>
  </Container>
);

export default CookiePolicyPage;
