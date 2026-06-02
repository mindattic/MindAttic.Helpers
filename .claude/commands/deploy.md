---
description: Deploy the MindAttic.Helpers landing page (mindattic.com/mindattichelpers.htm) via MindAttic.Deploy.
---

Render this repo's `README.md` through the MindAttic catalog template (Cyberspace theme) and FTPS-upload the single-file result to `mindattic.com/mindattichelpers.htm`.

Run from the sibling MindAttic.Deploy repo:

```bash
cd ../MindAttic.Deploy
npm run build  -- --only mindattichelpers --no-discover   # render out/mindattichelpers.htm
npm run deploy -- --only mindattichelpers                 # FTPS upload (Vault-held credentials)
```

The `mindattichelpers` entry already exists in `MindAttic.Deploy/projects.json` (`projects[]`, Cyberspace theme). For it to also appear on the mindattic.com homepage grid, the GitHub repo `mindattic/MindAttic.Helpers` must be public and tagged with the `software` topic.
