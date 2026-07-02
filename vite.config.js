import { defineConfig } from "vite";

// 戦国クイズ絵巻 - Vite 設定
// Fable が出力した src/App.fs.js を index.html から読み込み、
// Vite が react などの依存を解決してバンドルします。
export default defineConfig({
  root: ".",
  publicDir: "public",
  // 相対パスで出力。GitHub Pages のサブパス
  // (https://<user>.github.io/sengoku-kuizu/) でもローカルでも
  // アセットが正しく解決されます。
  base: "./",
  server: {
    port: 5173,
    host: true,
  },
  build: {
    outDir: "dist",
    emptyOutDir: true,
  },
});
