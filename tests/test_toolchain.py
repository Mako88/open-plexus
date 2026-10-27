"""The wheel index is the whole reason the card works; a torch without sm_61 fails here in
seconds instead of mid-run."""


def test_torch_targets_pascal():
    import torch

    assert torch.cuda.is_available()
    assert "sm_61" in torch.cuda.get_arch_list()
