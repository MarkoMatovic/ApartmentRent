# API kontrakt (OpenAPI)

`swagger.json` je snimak backend API šeme, a `src/shared/api/schema.d.ts` su
TypeScript tipovi generisani iz nje (`openapi-typescript`).

## Kako se regeneriše

1. Pokreni backend (`dotnet run` u `LandlordApp`, sluša na `http://localhost:5197`).
2. U `front-land` pokreni:

   ```bash
   npm run gen:api:fetch
   ```

   (skida svež `openapi/swagger.json` i regeneriše `schema.d.ts`)

Ako već imaš svež `swagger.json`, dovoljno je `npm run gen:api`.

## Zašto

Ručno pisani interfejsi u `src/shared/api/*.ts` su istorijski odlutali od
backenda (pogrešna imena polja, pogrešne rute) — generisani `schema.d.ts` je
izvor istine za proveru. Pri svakoj izmeni backend kontrolera/DTO-a regeneriši
šemu i uporedi sa API slojem koji diraš, npr:

```ts
import type { paths, components } from '../shared/api/schema';
type SavedSearchDto = components['schemas']['SavedSearchDto'];
```

Novi API kod piši direktno nad `schema.d.ts` tipovima umesto ručnih interfejsa.
