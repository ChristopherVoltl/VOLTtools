import xml.etree.ElementTree as ET
import json

def parse_origin(elem):
    xyz = [0, 0, 0]
    rpy = [0, 0, 0]
    if 'xyz' in elem.attrib:
        xyz = list(map(float, elem.attrib['xyz'].split()))
    if 'rpy' in elem.attrib:
        rpy = list(map(float, elem.attrib['rpy'].split()))
    return {'xyz': xyz, 'rpy': rpy}

def urdf_to_json(urdf_path):
    tree = ET.parse(urdf_path)
    root = tree.getroot()

    robot = {
        'name': root.attrib['name'],
        'links': {},
        'joints': {}
    }

    for link in root.findall('link'):
        name = link.attrib['name']
        visual = link.find('visual')
        if visual is not None:
            geom = visual.find('geometry')
            shape = None
            size = None
            if geom.find('box') is not None:
                shape = 'box'
                size = list(map(float, geom.find('box').attrib['size'].split()))
            elif geom.find('cylinder') is not None:
                shape = 'cylinder'
                size = {
                    'radius': float(geom.find('cylinder').attrib['radius']),
                    'length': float(geom.find('cylinder').attrib['length'])
                }
            elif geom.find('mesh') is not None:
                shape = 'mesh'
                size = geom.find('mesh').attrib.get('filename')

            origin = visual.find('origin')
            origin_data = parse_origin(origin) if origin is not None else None

            robot['links'][name] = {
                'visual': {
                    'geometry': {'shape': shape, 'size': size},
                    'origin': origin_data
                }
            }

    for joint in root.findall('joint'):
        name = joint.attrib['name']
        joint_type = joint.attrib['type']
        parent = joint.find('parent').attrib['link']
        child = joint.find('child').attrib['link']
        axis = joint.find('axis')
        axis_data = list(map(float, axis.attrib['xyz'].split())) if axis is not None else [0, 0, 0]

        origin = joint.find('origin')
        origin_data = parse_origin(origin) if origin is not None else None

        robot['joints'][name] = {
            'type': joint_type,
            'parent': parent,
            'child': child,
            'axis': axis_data,
            'origin': origin_data
        }

    return robot

# Convert and save
if __name__ == '__main__':
    import sys
    urdf_file = 'C:\\Users\\Chris\\source\\repos\\VOLTtools\\VOLTtools\\Resources\\ur_description\\urdf\\ur10e.urdf'
    out_file = urdf_file.replace('.urdf', '.json')

    model = urdf_to_json(urdf_file)

    with open(out_file, 'w') as f:
        json.dump(model, f, indent=2)

    print(f'Converted {urdf_file} -> {out_file}')
