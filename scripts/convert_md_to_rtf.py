import sys
import pypandoc

def convert_md_to_rtf(md_file, rtf_file):
    output = pypandoc.convert_file(md_file, 'rtf', format='md')
    with open(rtf_file, 'w') as f:
        f.write(output)

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python convert_md_to_rtf.py <input.md> <output.rtf>")
        sys.exit(1)
    
    md_file = sys.argv[1]
    rtf_file = sys.argv[2]
    convert_md_to_rtf(md_file, rtf_file)
