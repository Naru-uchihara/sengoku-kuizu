import { defineConfig } from "vite";

// 戦国クイズ絵巻 - Vite 設定
// Fable が出力した src/App.fs.js を index.html から読み込み、
// Vite が react などの依存を解決してバンドルします。
export default defineConfig({
  root: ".",
  publicDir: "public",
  server: {
    port: 5173,
    host: true,
  },
  build: {
    outDir: "dist",
    emptyOutDir: true,
  },
});
