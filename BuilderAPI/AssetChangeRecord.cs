using System;
using System.Collections.Generic;
using System.Text;

namespace BuilderAPI
{
    public record AssetChangeRecord<AssetType>(AssetType Asset, AssetProgressStatus Status );
}
