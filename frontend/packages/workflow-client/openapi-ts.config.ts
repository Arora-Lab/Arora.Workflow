import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: 'swagger.json',
  output: 'src/generated',
  client: '@hey-api/client-fetch'
});
