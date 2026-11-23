"""Plugin side of AnimationV8PluginCrossCheck: run between its Export and Import tests.

    python -I anim_v8_crosscheck.py <ANIM_XCHECK_DIR> <path to io_scene_rmv2/anim_format.py>

anim_format.py has no bpy dependency, so it is loaded on its own (the package's __init__ needs Blender) and this
runs on plain Python with numpy.
"""
import importlib.util
import json
import pathlib
import site
import sys

# -I leaves out the user site-packages, which is where numpy usually is
sys.path.append(site.getusersitepackages())
import numpy as np  # noqa: E402

folder = pathlib.Path(sys.argv[1])
spec = importlib.util.spec_from_file_location("anim_format", sys.argv[2])
anim_format = sys.modules["anim_format"] = importlib.util.module_from_spec(spec)
spec.loader.exec_module(anim_format)


def frames(part):
    out = [(part.static_frame, False)] if part.static_frame is not None else []
    return out + [(f, True) for f in part.dynamic_frames]


def edit(anim):
    # Same as AnimationV8PluginCrossCheck.Edit, with AE's float32 rounding of the result
    for part in anim.parts:
        for frame, _ in frames(part):
            frame.translations = (np.asarray(frame.translations, np.float32).astype(np.float64) * 1.5 + 0.1).astype(np.float32).astype(np.float64)
            frame.rotations = (np.asarray(frame.rotations, np.float32).astype(np.float64) * 0.9).astype(np.float32).astype(np.float64)


def dump(anim):
    def frame_dump(f):
        return {"T": np.asarray(f.translations, np.float64).tolist(), "R": np.asarray(f.rotations, np.float64).tolist()}
    return [{"Static": frame_dump(p.static_frame) if p.static_frame is not None else None,
             "Dynamic": [frame_dump(f) for f in p.dynamic_frames]} for p in anim.parts]


def tolerances(rates, ranges, dynamic, ranged):
    out = []
    for bone, stored in enumerate(rates):
        rate = int(stored) if dynamic else -int(stored)
        if rate <= 0:
            continue
        out.append(np.abs(np.asarray(ranges[bone][0], np.float64)) / 127.0 * 0.5 + 1e-6 if rate == ranged else None)
    return out


def compare(anim, expected):
    """anim (as the plugin read it) against AE's json, within one quantisation step."""
    if len(anim.parts) != len(expected):
        return f"{len(anim.parts)} parts, AE has {len(expected)}"
    for p, (part, exp) in enumerate(zip(anim.parts, expected)):
        if (part.static_frame is None) != (exp["Static"] is None):
            return f"part {p}: static frame presence differs"
        if len(part.dynamic_frames) != len(exp["Dynamic"]):
            return f"part {p}: {len(part.dynamic_frames)} frames, AE has {len(exp['Dynamic'])}"
        pairs = [(f, e, True) for f, e in zip(part.dynamic_frames, exp["Dynamic"])]
        if part.static_frame is not None:
            pairs.append((part.static_frame, exp["Static"], False))
        for frame, e, dynamic in pairs:
            for values, wanted, rates, ranges, ranged, what in (
                    (frame.translations, e["T"], part.translation_rates, part.translation_ranges, 3, "translation"),
                    (frame.rotations, e["R"], part.rotation_rates, part.rotation_ranges, 4, "rotation")):
                values = np.asarray(values, np.float64).reshape(len(wanted), -1) if len(wanted) else np.zeros((0,))
                tol = tolerances(rates, ranges, dynamic, ranged)
                if len(values) != len(wanted):
                    return f"part {p} {what}: {len(values)} channels, AE has {len(wanted)}"
                for i, (v, w) in enumerate(zip(values, wanted)):
                    w = np.asarray(w, np.float64)
                    t = tol[i] if tol[i] is not None else np.maximum(1.0 / 32767.0, np.abs(w) * 1e-6)
                    if np.any(np.abs(v - w) > t):
                        return f"part {p} {what} channel {i}: plugin {v.tolist()}, AE {w.tolist()}"
    return None


index = json.loads((folder / "index.json").read_text())
problems = []
for entry in index:
    name = entry["name"]
    orig = (folder / f"{name}.orig.anim").read_bytes()
    ae = (folder / f"{name}.ae.anim").read_bytes()

    anim = anim_format.load(orig)
    plugin_saved = anim_format.save(anim)
    if plugin_saved != orig:
        problems.append(f"{name}: plugin re-save differs from the original")
    if plugin_saved != ae:
        problems.append(f"{name}: plugin re-save and AE re-save differ")

    # AE saves (edited) -> plugin reads
    ae_edit = anim_format.load((folder / f"{name}.ae_edit.anim").read_bytes())
    error = compare(ae_edit, json.loads((folder / f"{name}.ae_edit.json").read_text()))
    if error:
        problems.append(f"{name}: plugin reading AE's edited save: {error}")

    # plugin saves (edited) -> AE reads, in the Import test
    edit(anim)
    (folder / f"{name}.py_edit.anim").write_bytes(anim_format.save(anim))
    (folder / f"{name}.py_edit.json").write_text(json.dumps(dump(anim)))

    # Both sides' edited saves, byte for byte
    if (folder / f"{name}.py_edit.anim").read_bytes() != (folder / f"{name}.ae_edit.anim").read_bytes():
        problems.append(f"{name}: edited saves differ between AE and the plugin")

print(f"{len(index)} files, {len(problems)} problems")
for line in problems:
    print("  " + line)
