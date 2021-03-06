﻿/*
 *  Licensed to the Apache Software Foundation (ASF) under one or more
 *  contributor license agreements.  See the NOTICE file distributed with
 *  this work for additional information regarding copyright ownership.
 *  The ASF licenses this file to You under the Apache License, Version 2.0
 *  (the "License"); you may not use this file except in compliance with
 *  the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *
 */

using System;

namespace NMigrations.Core
{
    /// <summary>
    /// Contains event information for the <see cref="Engine.BeforeMigration"/> event.
    /// </summary>
    public class BeforeMigrationEventArgs : EventArgs
    {
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BeforeMigrationEventArgs"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="migration">The migration.</param>
        /// <param name="direction">The direction.</param>
        public BeforeMigrationEventArgs(long version, IMigration migration, MigrationDirection direction)
        {
            Version = version;
            Migration = migration;
            Direction = direction;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the version of the migration.
        /// </summary>
        /// <value>The version.</value>
        public virtual long Version
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the migration.
        /// </summary>
        /// <value>The migration.</value>
        public virtual IMigration Migration
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the direction of the migration.
        /// </summary>
        /// <value>The direction.</value>
        public MigrationDirection Direction
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the whole migration process should be cancelled at this point.
        /// </summary>
        /// <value><c>true</c> if cancel; otherwise, <c>false</c>.</value>
        public bool Cancel
        {
            get;
            set;
        }

        #endregion
    }
}