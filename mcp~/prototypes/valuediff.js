// Compares the serialized VALUES of matching components between two baked prefabs.
// The structural diff (hierarchy/order/rects/component sets) cannot see a changed font size or a
// gradient direction — this closes that gap.
const fs = require('fs');

// Fields that differ between two prefab files without meaning anything: identity, cross-references, and
// TMP's cached render data (recomputed on load, never authored).
const IGNORE = /^(m_GameObject|m_ObjectHideFlags|m_CorrespondingSourceObject|m_PrefabInstance|m_PrefabAsset|m_EditorClassIdentifier|m_Name|serializedVersion|m_Script|m_textInfo|m_mesh|m_RenderedWidth|m_RenderedHeight|m_preferredWidth|m_preferredHeight|m_renderMode|m_havePropertiesChanged|m_isInputParsingRequired|m_isCalculateSizeRequired|m_isLayoutDirty|m_isAwake|m_Children|m_Father|m_RootOrder)/;

function load( path ) {
	const raw = fs.readFileSync(path, 'utf8');
	const go = {}, comp = {}, tr = {}, inst = {};
	for (const chunk of raw.split(/^--- /m).slice(1)) {
		const h = chunk.match(/^!u!(\d+) &(-?\d+)( stripped)?/);
		if (!h) continue;
		const [, cls, id] = h;
		const gid = (chunk.match(/m_GameObject: \{fileID: (-?\d+)\}/) || [])[1];
		if (cls === '1') {
			go[id] = { name: (chunk.match(/m_Name: (.*)/) || [])[1],
				comps: [...chunk.matchAll(/component: \{fileID: (-?\d+)\}/g)].map(m => m[1]) };
		} else if (cls === '1001') {
			inst[id] = { name: (chunk.match(/propertyPath: m_Name\n\s+value: (.*)/) || [])[1] };
		} else {
			comp[id] = { cls, go: gid, body: chunk,
				script: (chunk.match(/m_Script: \{fileID: \d+, guid: (\w+)/) || [])[1] };
		}
		if (cls === '224' || cls === '4') {
			const pi = (chunk.match(/m_PrefabInstance: \{fileID: (-?\d+)\}/) || [])[1];
			tr[id] = { go: gid, instance: pi && pi !== '0' ? pi : null,
				father: (chunk.match(/m_Father: \{fileID: (-?\d+)\}/) || [])[1],
				children: [...((chunk.match(/m_Children:\n((?:\s+- \{fileID: -?\d+\}\n)*)/) || [, ''])[1])
					.matchAll(/fileID: (-?\d+)/g)].map(m => m[1]) };
		}
	}
	const name = t => tr[t].instance ? ((inst[tr[t].instance] || {}).name || '(instance)') : ((go[tr[t].go] || {}).name || '?');
	const nodes = {};
	(function walk( t, path ) {
		const p = path ? `${path}/${name(t)}` : name(t);
		// Template instances have no plain component docs to compare — skip their bodies, keep walking.
		nodes[p] = (tr[t].instance ? [] : ((go[tr[t].go] || {}).comps || [])).map(c => comp[c]).filter(Boolean);
		for (const c of tr[t].children) if (tr[c]) walk(c, p);
	})(Object.keys(tr).find(t => tr[t].father === '0'), '');
	return nodes;
}

// A component body reduced to comparable "key: value" pairs.
function fields( body ) {
	const list = [];
	for (const line of body.split('\n')) {
		const m = line.match(/^\s+([A-Za-z0-9_]+): (.*)$/);
		if (!m) continue;
		const [, key, value] = m;
		if (IGNORE.test(key)) continue;
		if (/fileID:/.test(value)) continue; // cross-file references differ by construction
		list.push(`${key}: ${value.trim()}`);
	}
	return list;
}

const A = load(process.argv[2]); // yours
const B = load(process.argv[3]); // baked
const out = [];

for (const path of Object.keys(A)) {
	const a = A[path], b = B[path];
	if (!b) continue;
	for (const ca of a) {
		const cb = b.find(c => c.script ? c.script === ca.script : c.cls === ca.cls);
		if (!cb) continue;
		const fa = fields(ca.body), fb = fields(cb.body);
		const max = Math.max(fa.length, fb.length);
		for (let k = 0; k < max; k++) {
			if (fa[k] === fb[k]) continue;
			out.push(`${path}  [${ca.script ? ca.script.slice(0, 8) : 'cls' + ca.cls}]   yours: ${fa[k] ?? '—'}   baked: ${fb[k] ?? '—'}`);
		}
	}
}
console.log(out.length ? out.join('\n') : 'Alle Komponentenwerte identisch.');
