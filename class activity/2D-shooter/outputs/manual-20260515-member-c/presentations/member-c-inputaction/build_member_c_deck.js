const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
const pptxgen = require('pptxgenjs');

const OUT_DIR = '/Users/tianbo/Desktop/DI32002 Game Programming';
const WORK = '/Users/tianbo/Documents/GitHub/DI32002-Game-Programming/class activity/2D-shooter/outputs/manual-20260515-member-c/presentations/member-c-inputaction';
const ASSET_DIR = path.join(WORK, 'assets');
const FINAL = path.join(OUT_DIR, 'Member_C_InputAction_TimesNewRoman.pptx');

const W = 13.333;
const H = 7.5;
const FONT = 'Times New Roman';
const C = {
  bg: '10151F',
  panel: '172030',
  panel2: '0D1220',
  text: 'F2F5FA',
  muted: 'A8B1C3',
  accent: '46B8FF',
  accent2: 'F4B84A',
  green: '7EE787',
  red: 'FF6B6B',
  line: '2C3A52',
  codeBg: '0B1020',
};

function escXml(s) {
  return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

async function codePng(fileName, title, lines, highlightIdxs = []) {
  const width = 1540;
  const lineH = 42;
  const top = 88;
  const height = top + lines.length * lineH + 46;
  const chars = (s) => escXml(s)
    .replace(/ /g, '&#160;')
    .replace(/"/g, '&quot;');

  let body = '';
  for (let i = 0; i < lines.length; i++) {
    const y = top + i * lineH;
    if (highlightIdxs.includes(i)) {
      body += `<rect x="30" y="${y - 29}" width="1480" height="38" rx="6" fill="#1D3E5F" opacity="0.86"/>`;
    }
    body += `<text x="52" y="${y}" font-family="Times New Roman" font-size="30" fill="#C9D7EE">${String(i + 1).padStart(2, '0')}</text>`;
    body += `<text x="118" y="${y}" font-family="Times New Roman" font-size="31" fill="#F6F8FF">${chars(lines[i])}</text>`;
  }

  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
    <rect width="${width}" height="${height}" rx="26" fill="#0B1020"/>
    <rect x="0" y="0" width="${width}" height="62" rx="26" fill="#151C2B"/>
    <circle cx="38" cy="31" r="9" fill="#FF5F57"/><circle cx="68" cy="31" r="9" fill="#FFBD2E"/><circle cx="98" cy="31" r="9" fill="#28C840"/>
    <text x="130" y="40" font-family="Times New Roman" font-size="24" fill="#A8B1C3">${escXml(title)}</text>
    ${body}
  </svg>`;
  const out = path.join(ASSET_DIR, fileName);
  await sharp(Buffer.from(svg)).png().toFile(out);
  return out;
}

function addBg(slide, section='Member C · InputAction') {
  slide.background = { color: C.bg };
  slide.addShape('rect', { x: 0, y: 0, w: W, h: H, fill: { color: C.bg }, line: { color: C.bg } });
  slide.addShape('rect', { x: 0, y: 0, w: W, h: 0.12, fill: { color: C.accent }, line: { color: C.accent }, transparency: 10 });
  slide.addText(section, { x: 0.5, y: 0.25, w: 6, h: 0.28, fontFace: FONT, fontSize: 12, color: C.muted, margin: 0 });
}

function title(slide, t, st) {
  slide.addText(t, { x: 0.5, y: 0.58, w: 12.2, h: 0.58, fontFace: FONT, fontSize: 31, bold: true, color: C.text, margin: 0, breakLine: false, fit: 'shrink' });
  if (st) slide.addText(st, { x: 0.52, y: 1.18, w: 11.8, h: 0.4, fontFace: FONT, fontSize: 15.5, color: C.muted, margin: 0, fit: 'shrink' });
}

function label(slide, text, x, y, w, color=C.accent) {
  slide.addText(text, { x, y, w, h: 0.28, fontFace: FONT, fontSize: 11, color, bold: true, margin: 0 });
}

function pill(slide, text, x, y, w, color=C.panel, border=C.line, fontSize=17) {
  slide.addShape('roundRect', { x, y, w, h: 0.58, rectRadius: 0.06, fill: { color }, line: { color: border, width: 1 } });
  slide.addText(text, { x: x + 0.08, y: y + 0.14, w: w - 0.16, h: 0.28, fontFace: FONT, fontSize, color: C.text, bold: true, align: 'center', margin: 0, fit: 'shrink' });
}

function arrow(slide, x1, y1, x2, y2, color=C.accent) {
  slide.addShape('line', { x: x1, y: y1, w: x2 - x1, h: y2 - y1, line: { color, width: 2, beginArrowType: 'none', endArrowType: 'triangle' } });
}

function bullet(slide, text, x, y, w, emphasis=false) {
  slide.addText([{ text: '• ', options: { color: emphasis ? C.accent2 : C.accent, bold: true } }, { text, options: { color: C.text, bold: emphasis } }], {
    x, y, w, h: 0.38, fontFace: FONT, fontSize: 17, margin: 0, breakLine: false, fit: 'shrink'
  });
}

function addFooter(slide, n) {
  slide.addText(`${String(n).padStart(2, '0')} / 05`, { x: 11.95, y: 7.06, w: 0.86, h: 0.24, fontFace: FONT, fontSize: 10, color: C.muted, align: 'right', margin: 0 });
}

(async () => {
  const enablePng = await codePng('enable_disable_code.png', 'ShootingController.cs — InputAction lifecycle', [
    'private void OnEnable()',
    '{',
    '    fireAction.Enable();',
    '}',
    '',
    'private void OnDisable()',
    '{',
    '    fireAction.Disable();',
    '}'
  ], [2, 7]);

  const readPng = await codePng('readvalue_code.png', 'ShootingController.cs — polling the Fire action', [
    'private void Update()',
    '{',
    '    ProcessInput();',
    '}',
    '',
    'if (fireAction.ReadValue<float>() >= 1)',
    '{',
    '    Fire();',
    '}'
  ], [5, 7]);

  const pptx = new pptxgen();
  pptx.layout = 'LAYOUT_WIDE';
  pptx.author = 'Tianbo Cao';
  pptx.subject = 'Unity Event Driven Code Lab - Member C';
  pptx.title = 'Member C - New Input System and InputAction';
  pptx.company = 'Happy Unity';
  pptx.lang = 'zh-CN';
  pptx.theme = {
    headFontFace: FONT,
    bodyFontFace: FONT,
    lang: 'zh-CN'
  };
  pptx.defineLayout({ name: 'LAYOUT_WIDE', width: W, height: H });
  pptx.layout = 'LAYOUT_WIDE';

  // Slide 1
  let s = pptx.addSlide();
  addBg(s);
  title(s, 'New Input System 与 InputAction', 'C 部分 · 2分30秒：解释 fireAction 如何把「物理按键」变成「开火动作」');
  label(s, '我们在整条事件链中的位置', 0.66, 1.72, 4);
  const xs = [0.8, 2.75, 4.75, 6.85, 8.95, 10.95];
  const steps = ['按键输入', 'InputAction', 'Update 轮询', 'Fire()', 'Instantiate', '物理销毁'];
  for (let i = 0; i < steps.length; i++) {
    const active = i === 1;
    pill(s, steps[i], xs[i], 2.18, i === 1 ? 1.65 : 1.45, active ? C.accent : C.panel, active ? C.accent : C.line, active ? 16.5 : 14.5);
    if (i < steps.length - 1) arrow(s, xs[i] + (i===1?1.65:1.45) + 0.08, 2.47, xs[i+1] - 0.1, 2.47, active ? C.accent2 : C.line);
  }
  s.addShape('roundRect', { x: 1.0, y: 3.25, w: 11.2, h: 2.15, rectRadius: 0.08, fill: { color: C.panel }, line: { color: C.line, width: 1 } });
  s.addText('一句话目标', { x: 1.25, y: 3.55, w: 2, h: 0.3, fontFace: FONT, fontSize: 14, color: C.accent2, bold: true, margin: 0 });
  s.addText('InputAction 让代码只关心「Fire 这个动作」，不关心玩家到底按的是 Space、鼠标左键，还是手柄扳机。', { x: 1.25, y: 3.95, w: 10.6, h: 0.55, fontFace: FONT, fontSize: 20, color: C.text, bold: true, margin: 0, fit: 'shrink' });
  bullet(s, '这就是输入层面的解耦：动作名稳定，设备绑定可以换。', 1.25, 4.72, 10.4, true);
  addFooter(s, 1);
  s.addNotes(`C 部分开场：我负责的是整条链里的 InputAction。前面 A/B 已经讲了 Unity 回调和 Update 轮询，我这里解释为什么代码里不是直接写 Space，而是通过 fireAction 这个抽象动作来读取输入。`);

  // Slide 2
  s = pptx.addSlide();
  addBg(s);
  title(s, '为什么不用 Input.GetKey(KeyCode.Space)?', '旧写法把动作和设备绑死；InputAction 把它们拆开。');
  s.addShape('roundRect', { x: 0.75, y: 1.75, w: 5.55, h: 4.25, rectRadius: 0.08, fill: { color: C.panel2 }, line: { color: '3A2230', width: 1 } });
  s.addText('旧 Input Manager', { x: 1.05, y: 2.02, w: 4.7, h: 0.38, fontFace: FONT, fontSize: 23, bold: true, color: C.red, margin: 0 });
  s.addText('if (Input.GetKey(KeyCode.Space))\n{\n    Fire();\n}', { x: 1.05, y: 2.65, w: 4.7, h: 1.2, fontFace: FONT, fontSize: 22, color: C.text, margin: 0, breakLine: false, fit: 'shrink' });
  bullet(s, 'Space 写死在代码里', 1.05, 4.2, 4.7);
  bullet(s, '换手柄/触屏要改代码', 1.05, 4.65, 4.7);
  bullet(s, '玩家自定义按键很难扩展', 1.05, 5.1, 4.7);

  s.addShape('roundRect', { x: 7.0, y: 1.75, w: 5.55, h: 4.25, rectRadius: 0.08, fill: { color: C.panel }, line: { color: C.accent, width: 1 } });
  s.addText('新 Input System', { x: 7.3, y: 2.02, w: 4.7, h: 0.38, fontFace: FONT, fontSize: 23, bold: true, color: C.accent, margin: 0 });
  s.addText('public InputAction fireAction;\n\nif (fireAction.ReadValue<float>() >= 1)\n{\n    Fire();\n}', { x: 7.3, y: 2.55, w: 4.8, h: 1.55, fontFace: FONT, fontSize: 20.5, color: C.text, margin: 0, fit: 'shrink' });
  bullet(s, 'Fire 是逻辑动作，不是某个键', 7.3, 4.35, 4.9, true);
  bullet(s, '绑定在 Inspector 里配置', 7.3, 4.8, 4.9);
  bullet(s, '键盘、鼠标、手柄共用一条代码', 7.3, 5.25, 4.9);
  addFooter(s, 2);
  s.addNotes(`这一页讲对比。旧写法是 Input.GetKey(KeyCode.Space)，代码直接依赖 Space。新系统里代码只读 fireAction，Space、Mouse0、Gamepad Trigger 都可以在 Inspector 里作为 Binding 加进去。重点强调：代码稳定，输入设备可替换。`);

  // Slide 3
  s = pptx.addSlide();
  addBg(s);
  title(s, 'InputAction 也有生命周期', '谁打开，谁关掉；让输入资源跟组件状态同步。');
  s.addImage({ path: enablePng, x: 0.65, y: 1.7, w: 6.35, h: 4.25 });
  s.addShape('roundRect', { x: 7.35, y: 1.85, w: 4.95, h: 3.9, rectRadius: 0.08, fill: { color: C.panel }, line: { color: C.line, width: 1 } });
  s.addText('为什么要成对出现？', { x: 7.68, y: 2.15, w: 4.3, h: 0.35, fontFace: FONT, fontSize: 22, bold: true, color: C.text, margin: 0 });
  bullet(s, 'OnEnable：组件激活，开始监听 Fire', 7.68, 2.9, 4.15, true);
  bullet(s, 'OnDisable：组件失活，停止监听 Fire', 7.68, 3.42, 4.15, true);
  bullet(s, '避免菜单/死亡状态下仍响应输入', 7.68, 3.94, 4.15);
  bullet(s, '资源生命周期对齐组件生命周期', 7.68, 4.46, 4.15);
  s.addText('类比：像打开文件后必须关闭文件。InputAction 启用后也应该在组件失活时关闭。', { x: 7.68, y: 5.05, w: 4.2, h: 0.42, fontFace: FONT, fontSize: 14.2, italic: true, color: C.muted, margin: 0, fit: 'shrink' });
  addFooter(s, 3);
  s.addNotes(`这一页讲 Enable/Disable。不要把它讲成“玩家开火”。OnEnable 只是让 fireAction 开始监听输入；OnDisable 是释放/停止监听。这个点能体现事件驱动和资源管理：组件活着，输入才活着。`);

  // Slide 4
  s = pptx.addSlide();
  addBg(s);
  title(s, 'ReadValue<float>()：统一不同设备的输入值', '同一行代码，同时支持数字按键和模拟扳机。');
  s.addImage({ path: readPng, x: 0.65, y: 1.65, w: 6.35, h: 4.25 });
  s.addShape('roundRect', { x: 7.25, y: 1.78, w: 5.1, h: 4.18, rectRadius: 0.08, fill: { color: C.panel }, line: { color: C.line, width: 1 } });
  s.addText('Binding 列表（Inspector 中配置）', { x: 7.55, y: 2.08, w: 4.5, h: 0.32, fontFace: FONT, fontSize: 18.5, bold: true, color: C.accent, margin: 0 });
  const bindings = ['<Keyboard>/space', '<Mouse>/leftButton', '<Gamepad>/rightTrigger', '<Touchscreen>/primaryTouch/press'];
  bindings.forEach((b, i) => {
    s.addText('+ ' + b, { x: 7.75, y: 2.62 + i * 0.38, w: 4.2, h: 0.28, fontFace: FONT, fontSize: 16, color: C.text, margin: 0 });
  });
  s.addShape('line', { x: 7.55, y: 4.22, w: 4.45, h: 0, line: { color: C.line, width: 1 } });
  s.addText('返回值例子', { x: 7.55, y: 4.43, w: 1.8, h: 0.25, fontFace: FONT, fontSize: 14, bold: true, color: C.accent2, margin: 0 });
  s.addText('Space released  →  0.0\nSpace pressed    →  1.0\nTrigger halfway  →  0.5\nTrigger full     →  1.0', { x: 7.75, y: 4.78, w: 4.15, h: 0.95, fontFace: FONT, fontSize: 16, color: C.text, margin: 0, fit: 'shrink' });
  addFooter(s, 4);
  s.addNotes(`这一页讲 ReadValue<float>()。为什么不是 bool？因为新 Input System 要统一设备。键盘是 0 或 1；手柄扳机可能是 0 到 1 的连续值。这里用 >= 1 表示完全按下时才 Fire。`);

  // Slide 5
  s = pptx.addSlide();
  addBg(s);
  title(s, 'C 部分结论：输入抽象让射击逻辑更稳定', '把动作、设备、生命周期分开，是这一页最重要的 takeaway。');
  s.addShape('roundRect', { x: 0.9, y: 1.65, w: 11.55, h: 1.35, rectRadius: 0.08, fill: { color: C.panel }, line: { color: C.accent, width: 1 } });
  s.addText('OnEnable → fireAction.Enable() → Update → ReadValue<float>() → Fire() → OnDisable → fireAction.Disable()', { x: 1.15, y: 2.05, w: 11.0, h: 0.35, fontFace: FONT, fontSize: 19, bold: true, color: C.text, align: 'center', margin: 0, fit: 'shrink' });

  s.addText('讲给听众记住的三句话', { x: 1.0, y: 3.35, w: 5.2, h: 0.35, fontFace: FONT, fontSize: 22, bold: true, color: C.text, margin: 0 });
  bullet(s, 'InputAction = 逻辑动作，不是具体按键。', 1.05, 3.95, 5.8, true);
  bullet(s, 'Binding 在 Inspector 中配置，代码不用改。', 1.05, 4.48, 5.8);
  bullet(s, 'Enable/Disable 让输入监听跟组件生命周期同步。', 1.05, 5.01, 5.8);

  s.addShape('roundRect', { x: 7.25, y: 3.28, w: 4.9, h: 2.25, rectRadius: 0.08, fill: { color: C.panel2 }, line: { color: C.line, width: 1 } });
  s.addText('Sources', { x: 7.55, y: 3.58, w: 4.2, h: 0.3, fontFace: FONT, fontSize: 18, bold: true, color: C.accent2, margin: 0 });
  s.addText('Unity Input System API: InputAction\nUnity Manual: event execution order\nProject source: ShootingController.cs', { x: 7.55, y: 4.08, w: 4.2, h: 0.9, fontFace: FONT, fontSize: 14.5, color: C.muted, margin: 0, fit: 'shrink' });
  s.addText('Optional transition to D: once Fire() passes the input check, the next question is how Unity creates the projectile object at runtime.', { x: 7.55, y: 5.02, w: 4.1, h: 0.34, fontFace: FONT, fontSize: 12.4, italic: true, color: C.muted, margin: 0, fit: 'shrink' });
  addFooter(s, 5);
  s.addNotes(`收尾可以这样说：所以 C 部分的核心是，InputAction 把具体设备和游戏动作分开。ShootingController 只关心 Fire 动作有没有被触发，不关心 Space、鼠标还是手柄。下一位 D 同学就可以接上：Fire 通过之后，Unity 怎么 Instantiate 出真正的子弹对象。`);

  await pptx.writeFile({ fileName: FINAL });
  console.log(FINAL);
})();
