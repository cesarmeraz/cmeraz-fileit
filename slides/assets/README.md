# Slides Assets Convention

This folder contains media used by Marp decks under `slides/`.

## Structure

- `slides/assets/images/` for static images and screenshots (`.png`, `.jpg`, `.svg`)
- `slides/assets/gifs/` for short animated demos (`.gif`)
- `slides/assets/logs/` for exported log snapshots used in slides (`.png`, `.txt`)

## Naming

Use lowercase kebab-case names that reflect the slide topic.

Examples:

- `architecture-overview.png`
- `local-run-demo.gif`
- `correlation-log-timeline.png`

## Referencing in slides

Use project-root relative paths from the slide markdown.

```markdown
![w:1400](./slides/assets/images/architecture-overview.png)
```

```markdown
![w:1400](./slides/assets/gifs/local-run-demo.gif)
```
