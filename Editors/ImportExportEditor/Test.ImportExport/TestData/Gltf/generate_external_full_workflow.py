# SPDX-License-Identifier: CC0-1.0

import math
import sys
from pathlib import Path

import bpy


def create_image(name, pixels, color_space):
    image = bpy.data.images.new(name, width=2, height=2, alpha=True)
    image.pixels = pixels
    image.colorspace_settings.name = color_space
    return image


def create_material(name, base_image, normal_image, orm_image, *, masked):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    material.blend_method = "CLIP" if masked else "OPAQUE"
    material.alpha_threshold = 0.4

    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()

    output = nodes.new("ShaderNodeOutputMaterial")
    principled = nodes.new("ShaderNodeBsdfPrincipled")
    links.new(principled.outputs["BSDF"], output.inputs["Surface"])

    base_texture = nodes.new("ShaderNodeTexImage")
    base_texture.image = base_image
    links.new(base_texture.outputs["Color"], principled.inputs["Base Color"])
    if masked:
        alpha_clip = nodes.new("ShaderNodeMath")
        alpha_clip.operation = "GREATER_THAN"
        alpha_clip.inputs[1].default_value = 0.4
        links.new(base_texture.outputs["Alpha"], alpha_clip.inputs[0])
        links.new(alpha_clip.outputs[0], principled.inputs["Alpha"])
        principled.inputs["Emission Color"].default_value = (0.1, 0.05, 0.0, 1.0)
        principled.inputs["Emission Strength"].default_value = 1.0

    normal_texture = nodes.new("ShaderNodeTexImage")
    normal_texture.image = normal_image
    normal_map = nodes.new("ShaderNodeNormalMap")
    links.new(normal_texture.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], principled.inputs["Normal"])

    orm_texture = nodes.new("ShaderNodeTexImage")
    orm_texture.image = orm_image
    separate = nodes.new("ShaderNodeSeparateColor")
    links.new(orm_texture.outputs["Color"], separate.inputs["Color"])
    links.new(separate.outputs["Green"], principled.inputs["Roughness"])
    links.new(separate.outputs["Blue"], principled.inputs["Metallic"])

    occlusion_tree = bpy.data.node_groups.get("glTF Material Output")
    if occlusion_tree is None:
        occlusion_tree = bpy.data.node_groups.new("glTF Material Output", "ShaderNodeTree")
        occlusion_tree.interface.new_socket(
            name="Occlusion",
            in_out="INPUT",
            socket_type="NodeSocketFloat",
        )
    occlusion = nodes.new("ShaderNodeGroup")
    occlusion.node_tree = occlusion_tree
    links.new(separate.outputs["Red"], occlusion.inputs["Occlusion"])

    return material


def create_armature():
    armature_data = bpy.data.armatures.new("ExternalWorkflowArmature")
    armature = bpy.data.objects.new("ExternalWorkflowArmature", armature_data)
    bpy.context.collection.objects.link(armature)
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")

    bone_specs = [
        ("root", (0.0, 0.0, 0.0), (0.0, 0.0, 0.2), None),
        ("pelvis", (0.0, 0.0, 0.8), (0.0, 0.0, 1.2), "root"),
        ("head", (0.0, 0.0, 2.0), (0.0, 0.0, 2.2), "pelvis"),
        ("left_foot", (-0.25, 0.0, 0.0), (-0.25, 0.25, 0.0), "root"),
        ("right_foot", (0.25, 0.0, 0.0), (0.25, 0.25, 0.0), "root"),
        ("helper", (0.0, 0.0, 1.4), (0.0, 0.25, 1.4), "root"),
    ]
    edit_bones = {}
    for name, head, tail, parent_name in bone_specs:
        bone = armature_data.edit_bones.new(name)
        bone.head = head
        bone.tail = tail
        if parent_name is not None:
            bone.parent = edit_bones[parent_name]
        edit_bones[name] = bone

    bpy.ops.object.mode_set(mode="POSE")
    armature.scale = (2.0, 2.0, 2.0)
    create_actions(armature)
    bpy.ops.object.mode_set(mode="OBJECT")
    armature.select_set(False)
    return armature, [spec[0] for spec in bone_specs]


def create_actions(armature):
    armature.animation_data_create()

    move_action = bpy.data.actions.new("Move")
    armature.animation_data.action = move_action
    root = armature.pose.bones["root"]
    root.location = (0.0, 0.0, 0.0)
    root.keyframe_insert(data_path="location", frame=1)
    root.location = (0.0, 0.0, 0.25)
    root.keyframe_insert(data_path="location", frame=25)

    nod_action = bpy.data.actions.new("Nod")
    armature.animation_data.action = nod_action
    head = armature.pose.bones["head"]
    head.rotation_mode = "QUATERNION"
    head.rotation_quaternion = (1.0, 0.0, 0.0, 0.0)
    head.keyframe_insert(data_path="rotation_quaternion", frame=1)
    head.rotation_quaternion = (
        math.cos(math.radians(10.0)),
        math.sin(math.radians(10.0)),
        0.0,
        0.0,
    )
    head.keyframe_insert(data_path="rotation_quaternion", frame=25)
    armature.animation_data.action = move_action


def create_mesh(armature, bone_names, materials):
    mesh_data = bpy.data.meshes.new("ExternalWorkflowMesh")
    vertices = [
        (-0.6, 0.0, 0.0),
        (0.0, 0.0, 2.0),
        (0.6, 0.0, 0.0),
        (-0.5, 0.1, 0.2),
        (0.0, 0.1, 1.8),
        (0.5, 0.1, 0.2),
    ]
    faces = [(0, 1, 2), (3, 4, 5)]
    mesh_data.from_pydata(vertices, [], faces)
    mesh_data.update()
    mesh_data.polygons[0].material_index = 0
    mesh_data.polygons[1].material_index = 1

    uv_layer = mesh_data.uv_layers.new(name="UVMap")
    for loop in mesh_data.loops:
        vertex = mesh_data.vertices[loop.vertex_index]
        uv_layer.data[loop.index].uv = (
            vertex.co.x + 0.6,
            vertex.co.z * 0.5,
        )

    mesh = bpy.data.objects.new("ExternalWorkflowMesh", mesh_data)
    bpy.context.collection.objects.link(mesh)
    for material in materials:
        mesh_data.materials.append(material)

    modifier = mesh.modifiers.new("Armature", "ARMATURE")
    modifier.object = armature
    source_weights = [0.30, 0.25, 0.20, 0.10, 0.08, 0.07]
    for bone_name, weight in zip(bone_names, source_weights):
        group = mesh.vertex_groups.new(name=bone_name)
        group.add(range(len(vertices)), weight, "REPLACE")
    mesh.scale = (2.0, 2.0, 2.0)
    bpy.context.view_layer.objects.active = mesh
    mesh.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    mesh.select_set(False)
    world_matrix = mesh.matrix_world.copy()
    mesh.parent = armature
    mesh.matrix_world = world_matrix
    return mesh


def main(output_path):
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.render.fps = 24
    bpy.context.scene["license"] = "CC0-1.0"

    opaque_base = create_image(
        "OpaqueBase",
        [0.2, 0.6, 0.9, 1.0] * 4,
        "sRGB",
    )
    mask_base = create_image(
        "MaskBase",
        [
            0.8, 0.5, 0.1, 1.0,
            0.8, 0.5, 0.1, 0.0,
            0.8, 0.5, 0.1, 1.0,
            0.8, 0.5, 0.1, 0.0,
        ],
        "sRGB",
    )
    normal = create_image(
        "Normal",
        [0.5, 0.5, 1.0, 1.0] * 4,
        "Non-Color",
    )
    orm = create_image(
        "Orm",
        [0.8, 0.6, 0.2, 1.0] * 4,
        "Non-Color",
    )

    materials = [
        create_material("OpaqueMaterial", opaque_base, normal, orm, masked=False),
        create_material("MaskMaterial", mask_base, normal, orm, masked=True),
    ]
    armature, bone_names = create_armature()
    create_mesh(armature, bone_names, materials)

    bpy.ops.export_scene.gltf(
        filepath=str(output_path),
        export_format="GLB",
        export_extras=True,
        export_materials="EXPORT",
        export_normals=False,
        export_tangents=False,
        export_animations=True,
        export_animation_mode="ACTIONS",
        export_bake_animation=False,
        export_force_sampling=False,
        export_all_influences=True,
        export_influence_nb=8,
        export_morph=False,
        export_yup=True,
    )


if __name__ == "__main__":
    separator = sys.argv.index("--") if "--" in sys.argv else len(sys.argv)
    arguments = sys.argv[separator + 1:]
    output = (
        Path(arguments[0])
        if arguments
        else Path(__file__).with_name("external_full_workflow.glb")
    )
    main(output.resolve())
