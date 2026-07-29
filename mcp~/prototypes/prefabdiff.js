// Compares two baked prefabs structurally: hierarchy, child order, rects and component sets.
// Both sides come out of the same generator, so anything the baker or a style produces cancels out —
// only real differences remain. This is the instrument the description-vs-prefab diff could not be.
const fs = require('fs');

function load( path ) {
	const raw = fs.readFileSync(path, 'utf8');
	const go = {}, comp = {}, tr = {}, inst = {};
	for (const chunk of raw.split(/^--- /m).slice(1)) {
		const h = chunk.match(/^!u!(\d+) &(-?\d+)( stripped)?/);
		if (!h) continue;
		const [, cls, id, stripped] = h;
		const gid = (chunk.match(/m_GameObject: \{fileID: (-?\d+)\}/) || [])[1];
		if (cls === '1') {
			go[id] = { name: (chunk.match(/m_Name: (.*)/) || [])[1], active: (chunk.match(/m_IsActive: (\d)/) || [])[1],
				comps: [...chunk.matchAll(/component: \{fileID: (-?\d+)\}/g)].map(m => m[1]) };
		} else if (cls === '1001') {
			inst[id] = { source: (chunk.match(/m_SourcePrefab: \{fileID: \d+, guid: (\w+)/) || [])[1],
				name: (chunk.match(/propertyPath: m_Name\n\s+value: (.*)/) || [])[1] };
		} else {
			comp[id] = { cls, script: (chunk.match(/m_Script: \{fileID: \d+, guid: (\w+)/) || [])[1] };
		}
		if (cls === '224' || cls === '4') {
			tr[id] = { go: gid, stripped: !!stripped,
				// NOTE: every doc carries "m_PrefabInstance: {fileID: 0}" — 0 means "not an instance", and the
				// string "0" is truthy in JS, so it has to be normalised away or every node looks like a template.
				instance: ((chunk.match(/m_PrefabInstance: \{fileID: (-?\d+)\}/) || [])[1] || '0') === '0' ? null
					: (chunk.match(/m_PrefabInstance: \{fileID: (-?\d+)\}/) || [])[1],
				father: (chunk.match(/m_Father: \{fileID: (-?\d+)\}/) || [])[1],
				children: [...((chunk.match(/m_Children:\n((?:\s+- \{fileID: -?\d+\}\n)*)/) || [, ''])[1])
					.matchAll(/fileID: (-?\d+)/g)].map(m => m[1]),
				rect: {
					anchorMin: (chunk.match(/m_AnchorMin: \{x: ([-\d.eE]+), y: ([-\d.eE]+)\}/) || []).slice(1, 3),
					anchorMax: (chunk.match(/m_AnchorMax: \{x: ([-\d.eE]+), y: ([-\d.eE]+)\}/) || []).slice(1, 3),
					pivot: (chunk.match(/m_Pivot: \{x: ([-\d.eE]+), y: ([-\d.eE]+)\}/) || []).slice(1, 3),
					size: (chunk.match(/m_SizeDelta: \{x: ([-\d.eE]+), y: ([-\d.eE]+)\}/) || []).slice(1, 3),
					pos: (chunk.match(/m_AnchoredPosition: \{x: ([-\d.eE]+), y: ([-\d.eE]+)\}/) || []).slice(1, 3),
				} };
		}
	}
	const name = t => tr[t].instance ? ((inst[tr[t].instance] || {}).name || '(instance)') : ((go[tr[t].go] || {}).name || '?');
	function build( t ) {
		const node = { name: name(t), rect: tr[t].rect, children: tr[t].children.filter(c => tr[c]).map(build) };
		if (tr[t].instance) node.template = (inst[tr[t].instance] || {}).source;
		else node.comps = ((go[tr[t].go] || {}).comps || []).map(c => (comp[c] || {}).script || 'cls' + (comp[c] || {}).cls).sort();
		node.active = tr[t].instance ? '1' : (go[tr[t].go] || {}).active;
		return node;
	}
	return build(Object.keys(tr).find(t => tr[t].father === '0'));
}

const A = load(process.argv[2]); // reference (the human's version)
const B = load(process.argv[3]); // candidate (freshly baked from the description)

const out = [];
const num = v => v === undefined ? null : Math.round(parseFloat(v) * 100) / 100;

function walk( path, a, b ) {
	for (const key of ['anchorMin', 'anchorMax', 'pivot', 'size', 'pos']) {
		const av = (a.rect[key] || []).map(num), bv = (b.rect[key] || []).map(num);
		if (av.length === 2 && bv.length === 2 && (av[0] !== bv[0] || av[1] !== bv[1]))
			out.push(`RECT   ${path}.${key}: yours [${av}]  vs  baked [${bv}]`);
	}
	if (a.active !== b.active) out.push(`ACTIVE ${path}: yours ${a.active} vs baked ${b.active}`);

	if (a.comps && b.comps) {
		const missing = a.comps.filter(c => !b.comps.includes(c));
		const extra = b.comps.filter(c => !a.comps.includes(c));
		if (missing.length) out.push(`COMP-  ${path}: baked is MISSING ${missing.join(', ')}`);
		if (extra.length) out.push(`COMP+  ${path}: baked has EXTRA ${extra.join(', ')}`);
	}
	if (!!a.template !== !!b.template) out.push(`KIND   ${path}: template mismatch`);

	const an = a.children.map(c => c.name), bn = b.children.map(c => c.name);
	for (const n of an.filter(n => !bn.includes(n))) out.push(`MISSING ${path}/${n}   (in yours, not baked)`);
	for (const n of bn.filter(n => !an.includes(n))) out.push(`EXTRA   ${path}/${n}   (baked, not in yours)`);
	const ca = an.filter(n => bn.includes(n)), cb = bn.filter(n => an.includes(n));
	if (ca.join('|') !== cb.join('|')) out.push(`ORDER  ${path}: yours [${ca.join(' , ')}]  vs  baked [${cb.join(' , ')}]`);

	for (const child of a.children) {
		const match = b.children.find(c => c.name === child.name);
		if (match) walk(`${path}/${child.name}`, child, match);
	}
}
walk(A.name, A, B);
console.log(out.length ? out.join('\n') : 'IDENTISCH — keine strukturellen Unterschiede.');
