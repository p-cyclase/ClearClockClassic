import "./styles.css";

const app = document.querySelector<HTMLElement>("#app");

if (!app) {
  throw new Error("アプリケーションの表示領域が見つかりません。");
}

app.innerHTML = `
  <section class="placeholder" aria-labelledby="app-title">
    <p class="eyebrow">KURIKURO</p>
    <h1 id="app-title">くりくろ</h1>
    <p>現代版の開発をここから始めます。</p>
  </section>
`;
