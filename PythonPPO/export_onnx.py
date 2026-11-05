"""Kayitli actor'u egitimdeki hareket dagilimiyla Unity icin disa aktar."""
from pathlib import Path
import argparse
import torch
from torch import nn
from .actor import Actor
from .records import load_checkpoint

ROOT = Path(__file__).resolve().parent.parent

class UnityActor(nn.Module):
    def __init__(self, actor):
        super().__init__()
        self.actor = actor
        self.version = nn.Parameter(torch.tensor([3.]), requires_grad=False)
        self.memory = nn.Parameter(torch.tensor([0.]), requires_grad=False)
        self.action_size = nn.Parameter(torch.tensor([2.]), requires_grad=False)

    def forward(self, rays, vector):
        # Egitimdeki gozlem sirasi: 27 isin, sonra 4 vektor.
        mean, std = self.actor(torch.cat((rays, vector), dim=1))
        # Normal dagilimdan ornekle; actor.get_action ile ayni hareket dagilimi.
        action = torch.tanh(mean + std * torch.randn_like(mean)).clamp(-0.999999, 0.999999)
        return action, self.version, self.memory, self.action_size

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--checkpoint', required=True, type=Path)
    parser.add_argument('--output', type=Path, default=ROOT/'YazLab1/Assets/Models/ButtonAgent.onnx')
    args = parser.parse_args()
    checkpoint = load_checkpoint(args.checkpoint)
    actor = Actor().eval()
    actor.load_state_dict(checkpoint['actor'])
    model = UnityActor(actor).eval()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    torch.onnx.export(model, (torch.zeros(1,27), torch.zeros(1,4)), str(args.output),
        opset_version=11, input_names=['obs_0','obs_1'],
        output_names=['continuous_actions','version_number','memory_size','continuous_action_output_shape'],
        dynamic_axes={'obs_0':{0:'batch'},'obs_1':{0:'batch'},'continuous_actions':{0:'batch'}})
    import onnx
    onnx.checker.check_model(onnx.load(str(args.output)))
    print(f'Kayit: {checkpoint["steps"]} adim / {checkpoint["episodes"]} bolum; ONNX: {args.output}')

if __name__ == '__main__':
    main()
